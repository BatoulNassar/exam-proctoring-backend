using ExamProctoring.Application.Features.ExamAttempts.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    /// Persistence for the student's own answers.
    ///
    /// Deliberately narrow and attempt-scoped: there is no "all answers for an exam session"
    /// method, because the contract forbids exposing answers to any admin or export path until
    /// the exam session reaches CLOSED. Adding a broad read here would make that easy to
    /// violate by accident.
    public interface IStudentAnswerRepository
    {
        /// The student's persisted answers for one attempt, keyed by the per-attempt question
        /// public id so callers never handle raw Question ids.
        Task<List<PersistedAnswerView>> GetForAttemptAsync(int studentSessionId);

        /// Saves the answer and its idempotency record in ONE transaction, so a crash can never
        /// leave an applied answer without its record, or a recorded response without its answer.
        /// Handles the unique-key races described on <see cref="AnswerPersistOutcome"/>.
        Task<AnswerPersistResult> SaveWithIdempotencyAsync(AnswerPersistCommand command);
    }

    /// One stored answer, projected for the student read paths.
    public class PersistedAnswerView
    {
        /// AttemptQuestion.public_id - the id the student API speaks in.
        public Guid AttemptQuestionPublicId { get; set; }

        /// Canonical JSON; decoded by AnswerValueCodec before it reaches a DTO.
        public string StoredResponse { get; set; } = string.Empty;

        public int DurationMs { get; set; }
        public DateTime SavedAtUtc { get; set; }
    }

    /// Everything one atomic answer write needs. Assembled by the service after validation, so
    /// the repository performs no business decisions.
    public class AnswerPersistCommand
    {
        public int StudentSessionId { get; set; }

        /// The authored Question this answer belongs to - the StudentAnswer upsert key.
        public int QuestionId { get; set; }

        public string CanonicalResponse { get; set; } = string.Empty;
        public int DurationMs { get; set; }
        public DateTime? ClientAnsweredAtUtc { get; set; }

        /// Server receipt time; also the response's savedAtUtc.
        public DateTime NowUtc { get; set; }

        public string IdempotencyKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string ResourceKey { get; set; } = string.Empty;
        public byte[] RequestHash { get; set; } = Array.Empty<byte>();

        /// The frozen response to replay on a retry.
        public int ResponseStatus { get; set; }
        public string ResponseBody { get; set; } = string.Empty;
    }

    public enum AnswerPersistOutcome
    {
        /// This caller's write was applied and recorded.
        Applied,

        /// The key had already been used for a semantically identical request; the stored
        /// response is returned and nothing was written again.
        ReplayedExisting,

        /// The key had already been used for a different request.
        ConflictDifferentRequest,
    }

    public class AnswerPersistResult
    {
        public AnswerPersistOutcome Outcome { get; init; }

        /// Populated on ReplayedExisting - the original frozen response.
        public int ReplayedStatus { get; init; }
        public string? ReplayedBody { get; init; }

        public static AnswerPersistResult Applied() =>
            new() { Outcome = AnswerPersistOutcome.Applied };

        public static AnswerPersistResult Replay(int status, string body) =>
            new() { Outcome = AnswerPersistOutcome.ReplayedExisting, ReplayedStatus = status, ReplayedBody = body };

        public static AnswerPersistResult Conflict() =>
            new() { Outcome = AnswerPersistOutcome.ConflictDifferentRequest };
    }
}
