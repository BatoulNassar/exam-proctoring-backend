using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence.Repositories
{
    public class IdempotencyRepository : IIdempotencyRepository
    {
        private const int UniqueConstraintViolation = 2627;
        private const int UniqueIndexViolation = 2601;

        private readonly AppDbContext _context;

        public IdempotencyRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IdempotencyResult> FindAsync(
            int studentSessionId, string idempotencyKey, IdempotencyRequest request)
        {
            var existing = await LookupAsync(studentSessionId, idempotencyKey);

            return existing == null ? IdempotencyResult.None() : Evaluate(existing, request);
        }

        public async Task<IdempotencyResult> StoreAsync(
            int studentSessionId, string idempotencyKey, IdempotencyRequest request)
        {
            try
            {
                await _context.IdempotencyRecords.AddAsync(new IdempotencyRecord
                {
                    student_session_id = studentSessionId,
                    idempotency_key = idempotencyKey,
                    endpoint = request.Endpoint,
                    resource_key = request.ResourceKey,
                    request_hash = request.RequestHash,
                    response_status = request.ResponseStatus,
                    response_body = request.ResponseBody,
                    created_at_utc = request.NowUtc,
                    created_at = request.NowUtc,
                });

                await _context.SaveChangesAsync();

                return IdempotencyResult.None();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // A concurrent request recorded the same key first. Its record is authoritative;
                // this caller replays it or is told the key was reused.
                Detach();

                var winner = await LookupAsync(studentSessionId, idempotencyKey);

                // A null winner would mean the violation came from somewhere unexpected, and
                // reporting success would be a lie.
                if (winner == null)
                    throw;

                return Evaluate(winner, request);
            }
        }

        /// IgnoreQueryFilters is deliberate and load-bearing.
        ///
        /// IdempotencyRecord inherits BaseEntity, so the global soft-delete filter would hide an
        /// is_deleted row from this lookup while the database UNIQUE index still enforced it.
        /// That combination turns a legitimate retry into an unexplained insert failure, and only
        /// under a race. These rows are never soft-deleted; reading past the filter means a
        /// future accident cannot produce that behaviour either.
        private async Task<IdempotencyRecord?> LookupAsync(int studentSessionId, string idempotencyKey) =>
            await _context.IdempotencyRecords
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.student_session_id == studentSessionId
                                          && r.idempotency_key == idempotencyKey);

        /// Same key + same operation + same resource + same semantic content replays.
        /// Anything else is a reuse conflict - including the same key aimed at a different
        /// endpoint, which is how one key can never leak across Submit and PUT Answer.
        private static IdempotencyResult Evaluate(IdempotencyRecord record, IdempotencyRequest request)
        {
            var sameOperation = string.Equals(record.endpoint, request.Endpoint, StringComparison.Ordinal)
                                && string.Equals(record.resource_key, request.ResourceKey, StringComparison.OrdinalIgnoreCase);

            var sameRequest = record.request_hash.AsSpan().SequenceEqual(request.RequestHash);

            return sameOperation && sameRequest
                ? IdempotencyResult.Replay(record.response_status, record.response_body)
                : IdempotencyResult.Conflict();
        }

        private void Detach()
        {
            foreach (var entry in _context.ChangeTracker.Entries<IdempotencyRecord>().ToList())
                entry.State = EntityState.Detached;
        }

        private static bool IsUniqueViolation(DbUpdateException exception) =>
            exception.InnerException is SqlException sql
            && (sql.Number == UniqueConstraintViolation || sql.Number == UniqueIndexViolation);
    }
}
