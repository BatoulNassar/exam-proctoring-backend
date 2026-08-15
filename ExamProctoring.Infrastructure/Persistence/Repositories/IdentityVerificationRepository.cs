using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using ExamProctoring.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    /// Persistence for identity verification.
    ///
    /// Two things here are load-bearing and easy to break by "tidying":
    ///
    /// 1. <see cref="RecordAttemptAsync"/> runs inside an explicit transaction, and consumes
    ///    the attempt BEFORE claiming the clientAttemptId. That order makes the verification
    ///    session row the contention point, so a duplicate key rolls the consumption back
    ///    instead of leaving a student charged for an attempt that was never recorded.
    /// 2. Attempt rows are never soft-deleted and are read past the global query filter, for
    ///    the same reason as IdempotencyRecord: the filter would hide a row from the lookup
    ///    while the database still enforced its unique index, turning a legitimate retry into
    ///    an unexplained failure - and only under a race.
    public class IdentityVerificationRepository : IIdentityVerificationRepository
    {
        /// SQL Server unique-constraint / unique-index violations.
        private const int UniqueConstraintViolation = 2627;
        private const int UniqueIndexViolation = 2601;

        private readonly AppDbContext _context;

        public IdentityVerificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<VerificationSessionView?> GetByStudentSessionAsync(int studentSessionId) =>
            await Sessions().FirstOrDefaultAsync(s => s.StudentSessionId == studentSessionId);

        public async Task<VerificationSessionView?> GetByPublicIdAsync(int studentId, Guid publicId) =>
            // Ownership is part of the predicate, so a foreign session is simply not found
            // rather than being loaded and then compared.
            await Sessions().FirstOrDefaultAsync(s => s.PublicId == publicId && s.StudentId == studentId);

        private IQueryable<VerificationSessionView> Sessions() =>
            _context.IdentityVerificationSessions
                .AsNoTracking()
                .Select(s => new VerificationSessionView
                {
                    Id = s.id,
                    PublicId = s.public_id,
                    StudentSessionId = s.student_session_id,
                    ExamSessionId = s.StudentSession.exam_session_id,
                    StudentId = s.StudentSession.student_id,
                    Status = s.status,
                    AttemptsUsed = s.attempts_used,
                    MaxAttempts = s.max_attempts,
                    VerifiedAtUtc = s.verified_at_utc,
                });

        public async Task<(VerificationSessionView Session, bool WasCreated)> CreateAsync(
            int studentSessionId, int maxAttempts, string? deviceId, DateTime nowUtc)
        {
            var wasCreated = true;

            try
            {
                await _context.IdentityVerificationSessions.AddAsync(new IdentityVerificationSession
                {
                    student_session_id = studentSessionId,
                    status = IdentityVerificationStatus.Pending,
                    attempts_used = 0,
                    max_attempts = maxAttempts,
                    device_id = deviceId,
                    created_at = nowUtc,
                });

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // A concurrent request created it first. That row is authoritative - returning
                // it is what stops two simultaneous client opens from minting two budgets.
                // This caller did not create anything, so it must report a resume, not a 201.
                Detach();
                wasCreated = false;
            }

            var view = await GetByStudentSessionAsync(studentSessionId);

            // Null here would mean the violation came from somewhere unexpected, and reporting
            // a fabricated session would be worse than failing.
            if (view == null)
                throw new InvalidOperationException(
                    $"Identity verification session for student session {studentSessionId} could not be created or read back.");

            return (view, wasCreated);
        }

        public async Task<ReferenceFaceView?> GetReferenceFaceAsync(int studentId) =>
            await _context.Students
                .AsNoTracking()
                .Where(s => s.id == studentId)
                .Select(s => new ReferenceFaceView
                {
                    Embedding = s.reference_face_embedding,
                    Model = s.reference_face_model,
                    ModelVersion = s.reference_face_model_version,
                })
                .FirstOrDefaultAsync();

        public async Task<VerificationAttemptView?> FindAttemptAsync(
            int verificationSessionId, string clientAttemptId) =>
            await AttemptLookup(verificationSessionId, clientAttemptId).FirstOrDefaultAsync();

        /// IgnoreQueryFilters is deliberate - see the class remarks.
        private IQueryable<VerificationAttemptView> AttemptLookup(
            int verificationSessionId, string clientAttemptId) =>
            _context.IdentityVerificationAttempts
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(a => a.identity_verification_session_id == verificationSessionId
                            && a.client_attempt_id == clientAttemptId)
                .Select(a => new VerificationAttemptView
                {
                    Outcome = a.outcome,
                    AttemptNumber = a.attempt_number,
                    AttemptsRemainingAfter = a.attempts_remaining_after,
                    MatchScore = a.match_score,
                });

        public async Task<RecordAttemptResult> RecordAttemptAsync(RecordAttemptCommand command)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var session = await _context.IdentityVerificationSessions
                .AsNoTracking()
                .Where(s => s.id == command.VerificationSessionId)
                .Select(s => new { s.attempts_used, s.max_attempts, s.verified_at_utc })
                .FirstOrDefaultAsync();

            if (session == null)
                throw new InvalidOperationException(
                    $"Identity verification session {command.VerificationSessionId} no longer exists.");

            // A concurrent attempt verified it between the service's check and here.
            if (session.verified_at_utc.HasValue)
            {
                await transaction.RollbackAsync();
                return new RecordAttemptResult { Status = RecordAttemptStatus.AlreadyVerified };
            }

            var attemptsUsed = session.attempts_used;

            if (command.ConsumesAttempt)
            {
                // One conditional UPDATE evaluated by SQL Server against the current row. Two
                // concurrent attempts cannot both satisfy "attempts_used < max_attempts" on the
                // last remaining try, so exactly one is charged and the other is refused.
                var consumed = await _context.IdentityVerificationSessions
                    .Where(s => s.id == command.VerificationSessionId
                                && s.attempts_used < s.max_attempts
                                && s.verified_at_utc == null)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(s => s.attempts_used, s => s.attempts_used + 1)
                        .SetProperty(s => s.updated_at, command.NowUtc));

                if (consumed == 0)
                {
                    await transaction.RollbackAsync();
                    return new RecordAttemptResult { Status = RecordAttemptStatus.NoAttemptsRemaining };
                }

                attemptsUsed += 1;
            }

            var attemptsRemaining = Math.Max(0, session.max_attempts - attemptsUsed);

            try
            {
                await _context.IdentityVerificationAttempts.AddAsync(new IdentityVerificationAttempt
                {
                    identity_verification_session_id = command.VerificationSessionId,
                    client_attempt_id = command.ClientAttemptId,

                    outcome = command.Outcome,
                    attempt_number = attemptsUsed,
                    attempts_remaining_after = attemptsRemaining,
                    match_score = command.MatchScore,
                    threshold_used = command.ThresholdUsed,

                    liveness_accepted = command.LivenessAccepted,
                    liveness_blink_count = command.LivenessBlinkCount,
                    liveness_frames_analysed = command.LivenessFramesAnalysed,
                    liveness_duration_ms = command.LivenessDurationMs,
                    liveness_min_eye_openness = command.LivenessMinEyeOpenness,
                    liveness_max_eye_openness = command.LivenessMaxEyeOpenness,
                    liveness_rejection_reason = command.LivenessRejectionReason,

                    embedding_model = command.EmbeddingModel,
                    embedding_model_version = command.EmbeddingModelVersion,

                    captured_at_utc = command.CapturedAtUtc,
                    attempted_at_utc = command.NowUtc,
                    created_at = command.NowUtc,
                });

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // This clientAttemptId was already recorded. Rolling back undoes the
                // consumption above, so a retry genuinely costs nothing.
                //
                // The winner is guaranteed committed by the time this fires: SQL Server blocks
                // the duplicate insert on the unique index until the first transaction ends.
                Detach();
                await transaction.RollbackAsync();

                var winner = await AttemptLookup(
                    command.VerificationSessionId, command.ClientAttemptId).FirstOrDefaultAsync();

                if (winner == null)
                    throw;

                return new RecordAttemptResult
                {
                    Status = RecordAttemptStatus.Replayed,
                    Attempt = winner,
                };
            }

            if (command.Outcome == IdentityVerificationOutcome.MATCHED)
                await MarkVerifiedAsync(command);

            await transaction.CommitAsync();

            return new RecordAttemptResult
            {
                Status = RecordAttemptStatus.Recorded,
                Attempt = new VerificationAttemptView
                {
                    Outcome = command.Outcome,
                    AttemptNumber = attemptsUsed,
                    AttemptsRemainingAfter = attemptsRemaining,
                    MatchScore = command.MatchScore,
                },
            };
        }

        /// Writes the identity result to both places in the same transaction as the attempt.
        ///
        /// StudentSession is updated as well as the verification session because that row is
        /// what IIdentityGate reads and what the proctor dashboard already displays. Both
        /// updates are guarded on the field still being null so a race settles on one winner.
        private async Task MarkVerifiedAsync(RecordAttemptCommand command)
        {
            await _context.IdentityVerificationSessions
                .Where(s => s.id == command.VerificationSessionId && s.verified_at_utc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.status, IdentityVerificationStatus.Verified)
                    .SetProperty(s => s.verified_at_utc, command.NowUtc)
                    .SetProperty(s => s.updated_at, command.NowUtc));

            await _context.StudentSessions
                .Where(ss => ss.id == command.StudentSessionId && ss.verified_at == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(ss => ss.verified_at, command.NowUtc)
                    .SetProperty(ss => ss.face_match_passed, true)
                    .SetProperty(ss => ss.liveness_passed, true)
                    .SetProperty(ss => ss.updated_at, command.NowUtc));
        }

        private void Detach()
        {
            foreach (var entry in _context.ChangeTracker
                         .Entries<IdentityVerificationAttempt>().ToList())
                entry.State = EntityState.Detached;

            foreach (var entry in _context.ChangeTracker
                         .Entries<IdentityVerificationSession>().ToList())
                entry.State = EntityState.Detached;
        }

        private static bool IsUniqueViolation(DbUpdateException exception) =>
            exception.InnerException is SqlException sql
            && (sql.Number == UniqueConstraintViolation || sql.Number == UniqueIndexViolation);
    }
}
