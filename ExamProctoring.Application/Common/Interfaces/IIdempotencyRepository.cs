using System;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    /// Generic durable idempotency, shared by every idempotent attempt endpoint.
    ///
    /// IMPORTANT: IdempotencyRecord inherits BaseEntity, so the global soft-delete query filter
    /// applies to reads while the database UNIQUE index still sees every row. Implementations
    /// MUST read with the filter disabled, otherwise a soft-deleted row would be invisible to a
    /// lookup and yet still reject the insert - surfacing as an unexplained failure only under a
    /// race. These rows are never soft-deleted, and the reads defend against it anyway.
    public interface IIdempotencyRepository
    {
        /// Looks up a previously recorded response for this attempt and key.
        Task<IdempotencyResult> FindAsync(int studentSessionId, string idempotencyKey, IdempotencyRequest request);

        /// Records the response for this attempt and key. Tolerates a concurrent insert of the
        /// same key by falling back to the winner's record.
        Task<IdempotencyResult> StoreAsync(int studentSessionId, string idempotencyKey, IdempotencyRequest request);
    }

    /// The identity of one idempotent request: which operation, which resource, and a hash of
    /// the semantic content. All three participate in deciding replay versus conflict.
    public class IdempotencyRequest
    {
        public string Endpoint { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
        public byte[] RequestHash { get; set; } = Array.Empty<byte>();
        public int ResponseStatus { get; set; }
        public string ResponseBody { get; set; } = string.Empty;
        public DateTime NowUtc { get; set; }
    }

    public enum IdempotencyOutcome
    {
        /// No record existed. For StoreAsync, this caller wrote it.
        None,

        /// A record exists for the same operation, resource and semantic content.
        Replay,

        /// A record exists for this key but a different request.
        Conflict,
    }

    public class IdempotencyResult
    {
        public IdempotencyOutcome Outcome { get; init; }
        public int ReplayedStatus { get; init; }
        public string? ReplayedBody { get; init; }

        public static IdempotencyResult None() => new() { Outcome = IdempotencyOutcome.None };

        public static IdempotencyResult Replay(int status, string body) =>
            new() { Outcome = IdempotencyOutcome.Replay, ReplayedStatus = status, ReplayedBody = body };

        public static IdempotencyResult Conflict() => new() { Outcome = IdempotencyOutcome.Conflict };
    }
}
