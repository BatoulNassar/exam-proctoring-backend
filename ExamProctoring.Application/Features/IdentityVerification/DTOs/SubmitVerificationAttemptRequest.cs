using System.Collections.Generic;

namespace ExamProctoring.Application.Features.IdentityVerification.DTOs
{
    /// Body of POST /api/v1/identity/verification-sessions/{id}/attempts - contract §3.1.
    /// Every value is client-supplied and none of it is trusted.
    public class SubmitVerificationAttemptRequest
    {
        /// Exactly 128 SFace values. Bound as double so a JSON number too large for float
        /// still reaches validation and is reported as an invalid embedding rather than
        /// silently becoming Infinity during binding.
        ///
        /// Never logged, never echoed, never persisted.
        public List<double>? Embedding { get; set; }

        public string? EmbeddingModel { get; set; }

        public string? EmbeddingVersion { get; set; }

        public LivenessEvidenceDto? Liveness { get; set; }

        /// The student's device clock, ISO-8601 with an explicit UTC designator. Audit only -
        /// it never influences any decision. Bound as a string so a malformed value is
        /// reported through the project's ApiResponse envelope rather than framework
        /// ProblemDetails.
        public string? CapturedAtUtc { get; set; }

        /// Idempotency key (contract §3.2). A retry after a lost response must return the
        /// original result and must not consume a second attempt.
        public string? ClientAttemptId { get; set; }

        /// Present only when policy.requiresSnapshotOnFailure is true, which is locked false
        /// for v1. Accepted for contract compatibility and deliberately never stored: this
        /// backend introduces no photo storage for failed attempts.
        public string? SnapshotJpegBase64 { get; set; }
    }

    /// Client-computed blink evidence. Claims to be validated, not facts - contract §3.4.
    public class LivenessEvidenceDto
    {
        public int? BlinkCount { get; set; }
        public int? FramesAnalysed { get; set; }
        public int? DurationMs { get; set; }
        public double? MinEyeOpenness { get; set; }
        public double? MaxEyeOpenness { get; set; }
    }
}
