using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.ExamAttempts.Services;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using ExamProctoring.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class AttemptFinalisationRepository : IAttemptFinalisationRepository
    {
        private const int UniqueConstraintViolation = 2627;
        private const int UniqueIndexViolation = 2601;

        private readonly AppDbContext _context;

        public AttemptFinalisationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AttemptFinalSnapshot?> GetSnapshotAsync(int studentSessionId) =>
            await _context.StudentSessions
                .AsNoTracking()
                .Where(ss => ss.id == studentSessionId && ss.finalised_at != null)
                .Select(ss => new AttemptFinalSnapshot
                {
                    AttemptPublicId = ss.public_id,
                    Status = ss.status,
                    Reason = ss.finalisation_reason!.Value,
                    FinalisedAtUtc = ss.finalised_at!.Value,
                    AnsweredCount = ss.answered_count ?? 0,
                    QuestionCount = ss.question_count ?? 0,
                    ReceiptCode = ss.receipt_code ?? string.Empty,
                })
                .FirstOrDefaultAsync();

        /// One conditional UPDATE guarded by "finalised_at IS NULL" is the whole concurrency
        /// story: student submit, automatic expiry and proctor termination can all attempt this
        /// simultaneously, and SQL Server lets exactly one of them through. Every loser reads the
        /// winner's snapshot, so there is one terminal result, one receipt and one finalised_at
        /// no matter how the calls interleave.
        ///
        /// The audit row is added before the update and saved inside the same transaction, so a
        /// crash cannot produce a finalised attempt with no audit trail or vice versa.
        public async Task<FinalisationPersistResult> FinaliseAsync(FinalisationCommand command)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var rowsAffected = await _context.StudentSessions
                    .Where(ss => ss.id == command.StudentSessionId && ss.finalised_at == null)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(ss => ss.finalised_at, command.NowUtc)
                        .SetProperty(ss => ss.finalisation_reason, command.Reason)
                        .SetProperty(ss => ss.answered_count, command.AnsweredCount)
                        .SetProperty(ss => ss.receipt_code, command.ReceiptCode)
                        .SetProperty(ss => ss.status, command.TargetStatus)
                        .SetProperty(ss => ss.submitted_at, ss => command.SubmittedAtUtc ?? ss.submitted_at)
                        // question_count is normally frozen at Start; coalesced so an attempt that
                        // somehow lacks it still finalises with a usable total.
                        .SetProperty(ss => ss.question_count, ss => ss.question_count ?? command.QuestionCount)
                        .SetProperty(ss => ss.updated_at, command.NowUtc));

                if (rowsAffected == 0)
                {
                    // Another trigger claimed it. Nothing of ours was written.
                    await transaction.RollbackAsync();

                    var winner = await GetSnapshotAsync(command.StudentSessionId);

                    // A null winner would mean the row vanished, which is not a race we can paper
                    // over.
                    if (winner == null)
                        throw new InvalidOperationException(
                            $"Attempt {command.StudentSessionId} could not be finalised and has no frozen result.");

                    return FinalisationPersistResult.AlreadyFinalised(winner);
                }

                await _context.AuditLogs.AddAsync(new AuditLog
                {
                    exam_session_id = command.ExamSessionId,
                    actor_id = command.AuditActorId,
                    actor_type = command.AuditActorType,
                    action = command.AuditAction,
                    entity_id = command.StudentSessionId,
                    entity_type = nameof(StudentSession),
                    details = command.AuditDetails,
                    occurred_at = command.NowUtc,
                    created_at = command.NowUtc,
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var snapshot = await GetSnapshotAsync(command.StudentSessionId);

                return FinalisationPersistResult.Finalised(snapshot!);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // The only unique constraint this operation can hit is receipt_code. Detach the
                // audit row so the retry starts clean, and let the service regenerate.
                await transaction.RollbackAsync();
                DetachAuditRows();

                throw new ReceiptCodeCollisionException(
                    $"Receipt code '{command.ReceiptCode}' already exists.");
            }
            catch (SqlException sql) when (sql.Number is UniqueConstraintViolation or UniqueIndexViolation)
            {
                // Same collision surfaced directly by ExecuteUpdate rather than wrapped.
                await transaction.RollbackAsync();
                DetachAuditRows();

                throw new ReceiptCodeCollisionException(
                    $"Receipt code '{command.ReceiptCode}' already exists.");
            }
        }

        /// Still running, deadline passed, not yet frozen. Ordered oldest-first so the most
        /// overdue attempts are cleared first when a batch is capped.
        public async Task<List<ExpiredAttemptView>> GetExpiredAttemptsAsync(DateTime nowUtc, int batchSize) =>
            await _context.StudentSessions
                .AsNoTracking()
                .Where(ss => ss.status == StudentSessionStatus.InExam
                             && ss.finalised_at == null
                             && ss.ends_at != null
                             && ss.ends_at <= nowUtc)
                .OrderBy(ss => ss.ends_at)
                .Take(batchSize)
                .Select(ss => new ExpiredAttemptView
                {
                    StudentSessionId = ss.id,
                    ExamSessionId = ss.exam_session_id,
                    CourseTag = ss.ExamSession.course_tag,
                    QuestionCount = ss.question_count ?? 0,
                })
                .ToListAsync();

        /// Only AuditLog is ever tracked here and it owns no collections, so there is no entity
        /// graph to walk and nothing to mutate while enumerating.
        private void DetachAuditRows()
        {
            foreach (var entry in _context.ChangeTracker.Entries<AuditLog>().ToList())
                entry.State = EntityState.Detached;
        }

        private static bool IsUniqueViolation(DbUpdateException exception) =>
            exception.InnerException is SqlException sql
            && (sql.Number == UniqueConstraintViolation || sql.Number == UniqueIndexViolation);
    }
}
