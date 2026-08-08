using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class StudentAnswerRepository : IStudentAnswerRepository
    {
        /// SQL Server unique-constraint / unique-index violations.
        private const int UniqueConstraintViolation = 2627;
        private const int UniqueIndexViolation = 2601;

        private readonly AppDbContext _context;

        public StudentAnswerRepository(AppDbContext context)
        {
            _context = context;
        }

        /// Joined through AttemptQuestion so the caller receives the student-facing question id
        /// and never has to know about raw Question ids. Answers with no matching materialised
        /// question (impossible today) are dropped rather than surfaced with a wrong key.
        public async Task<List<PersistedAnswerView>> GetForAttemptAsync(int studentSessionId) =>
            await (from answer in _context.StudentAnswers.AsNoTracking()
                   join attemptQuestion in _context.AttemptQuestions.AsNoTracking()
                       on new { answer.student_session_id, answer.question_id }
                       equals new { attemptQuestion.student_session_id, attemptQuestion.question_id }
                   where answer.student_session_id == studentSessionId
                   orderby attemptQuestion.ordinal
                   select new PersistedAnswerView
                   {
                       AttemptQuestionPublicId = attemptQuestion.public_id,
                       StoredResponse = answer.student_response,
                       DurationMs = answer.duration_ms,
                       SavedAtUtc = answer.saved_at,
                   })
                .ToListAsync();

        /// The answer write and its idempotency record commit together or not at all.
        ///
        /// Two distinct races are possible and both are handled without a 500:
        ///  - same key twice (a genuine client retry): the idempotency unique index rejects the
        ///    loser, which then re-reads the winner and replays or conflicts;
        ///  - different keys, same question (two legitimate writes): the StudentAnswer unique
        ///    index rejects the second insert, which falls back to an update.
        public async Task<AnswerPersistResult> SaveWithIdempotencyAsync(AnswerPersistCommand command)
        {
            // Fast path: an already-recorded key never re-enters the transaction at all.
            var existing = await FindRecordAsync(command);
            if (existing != null)
                return Evaluate(existing, command);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await UpsertAnswerAsync(command);

                var record = new IdempotencyRecord
                {
                    student_session_id = command.StudentSessionId,
                    idempotency_key = command.IdempotencyKey,
                    endpoint = command.Endpoint,
                    resource_key = command.ResourceKey,
                    request_hash = command.RequestHash,
                    response_status = command.ResponseStatus,
                    response_body = command.ResponseBody,
                    created_at_utc = command.NowUtc,
                    created_at = command.NowUtc,
                };

                await _context.IdempotencyRecords.AddAsync(record);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return AnswerPersistResult.Applied();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Another request recorded this key first. Rolling back also undoes this
                // caller's answer write, which is correct: the winner's write is the one that
                // stands, and the loser is about to replay the winner's response.
                await transaction.RollbackAsync();
                DetachTrackedRecords();

                var winner = await FindRecordAsync(command);

                // A null winner would mean the violation came from somewhere unexpected;
                // surfacing it is better than silently reporting success.
                if (winner == null)
                    throw;

                return Evaluate(winner, command);
            }
        }

        /// Update-then-insert rather than load-modify-save: this touches no tracked entity
        /// graph, so a unique-violation rollback cannot leave a half-built object graph behind.
        private async Task UpsertAnswerAsync(AnswerPersistCommand command)
        {
            var updated = await _context.StudentAnswers
                .Where(a => a.student_session_id == command.StudentSessionId
                            && a.question_id == command.QuestionId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(a => a.student_response, command.CanonicalResponse)
                    .SetProperty(a => a.duration_ms, command.DurationMs)
                    .SetProperty(a => a.client_answered_at, command.ClientAnsweredAtUtc)
                    .SetProperty(a => a.saved_at, command.NowUtc)
                    .SetProperty(a => a.updated_at, command.NowUtc));

            if (updated > 0)
                return;

            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO StudentAnswer
    (student_session_id, question_id, student_response, saved_at, duration_ms, client_answered_at, created_at, is_deleted)
VALUES
    ({command.StudentSessionId}, {command.QuestionId}, {command.CanonicalResponse}, {command.NowUtc},
     {command.DurationMs}, {command.ClientAnsweredAtUtc}, {command.NowUtc}, 0)");
            }
            catch (SqlException ex) when (ex.Number is UniqueConstraintViolation or UniqueIndexViolation)
            {
                // A concurrent write inserted the row between the update and the insert. The
                // row now exists, so the same write becomes an update. Last write wins.
                await _context.StudentAnswers
                    .Where(a => a.student_session_id == command.StudentSessionId
                                && a.question_id == command.QuestionId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(a => a.student_response, command.CanonicalResponse)
                        .SetProperty(a => a.duration_ms, command.DurationMs)
                        .SetProperty(a => a.client_answered_at, command.ClientAnsweredAtUtc)
                        .SetProperty(a => a.saved_at, command.NowUtc)
                        .SetProperty(a => a.updated_at, command.NowUtc));
            }
        }

        /// IgnoreQueryFilters is deliberate: IdempotencyRecord inherits BaseEntity, so the global
        /// soft-delete filter would hide an is_deleted row from this lookup while the database
        /// UNIQUE index still enforced it - turning a legitimate retry into an unexplained insert
        /// failure, and only under a race. These rows are never soft-deleted; reading past the
        /// filter means a future accident cannot produce that behaviour either.
        private async Task<IdempotencyRecord?> FindRecordAsync(AnswerPersistCommand command) =>
            await _context.IdempotencyRecords
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.student_session_id == command.StudentSessionId
                                          && r.idempotency_key == command.IdempotencyKey);

        /// Same key + same semantic request replays; same key + anything else conflicts.
        /// The resource is compared as well as the hash so reusing one key on a different
        /// question is a conflict rather than an accidental replay.
        private static AnswerPersistResult Evaluate(IdempotencyRecord record, AnswerPersistCommand command)
        {
            var sameResource = string.Equals(record.endpoint, command.Endpoint, StringComparison.Ordinal)
                               && string.Equals(record.resource_key, command.ResourceKey, StringComparison.OrdinalIgnoreCase);

            var sameRequest = record.request_hash.AsSpan().SequenceEqual(command.RequestHash);

            return sameResource && sameRequest
                ? AnswerPersistResult.Replay(record.response_status, record.response_body)
                : AnswerPersistResult.Conflict();
        }

        /// Only the idempotency record is ever tracked here, and it owns no collections - so
        /// there is no entity graph to walk and nothing to mutate while enumerating.
        private void DetachTrackedRecords()
        {
            foreach (var entry in _context.ChangeTracker.Entries<IdempotencyRecord>().ToList())
                entry.State = EntityState.Detached;
        }

        private static bool IsUniqueViolation(DbUpdateException exception) =>
            exception.InnerException is SqlException sql
            && (sql.Number == UniqueConstraintViolation || sql.Number == UniqueIndexViolation);
    }
}
