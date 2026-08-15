using System;

namespace ExamProctoring.Application.Features.IdentityVerification.DTOs
{
    /// Body of POST /api/v1/identity/verification-sessions.
    /// Identical shape for 201 (new) and 200 (resumed) - contract §2.2.
    public class VerificationSessionResponse
    {
        /// Opaque verification session UUID. Every attempt is posted against this.
        public Guid VerificationSessionId { get; set; }

        /// Integer ExamSession id, consistent with GET /sessions/eligibility,
        /// POST /device-checks and the exam-attempt endpoints, which already expose int
        /// session ids to this client. The contract example shows a UUID, but this system's
        /// exam sessions have never had one and the client already handles the int form.
        public int ExamSessionId { get; set; }

        public VerificationPolicyDto Policy { get; set; } = new();
    }

    /// Contract §2.2 policy block. Everything the client needs to run the camera stage.
    public class VerificationPolicyDto
    {
        /// Frozen at verification-session creation, so a mid-exam settings edit cannot change
        /// a budget the student has already been shown.
        public int MaxAttempts { get; set; }

        /// Already consumed. The client shows MaxAttempts - AttemptsUsed and, when the two are
        /// equal, goes straight to the proctor without opening the camera.
        public int AttemptsUsed { get; set; }

        public int RequiredBlinks { get; set; }

        /// Locked false for v1: camera frames never leave the student's device for normal
        /// matching (SRS §2.4), so no failed-attempt snapshot is requested or stored.
        public bool RequiresSnapshotOnFailure { get; set; }

        /// The client refuses to submit when either of these does not match its own build.
        /// Enforced server-side too - the client's refusal is not a control the backend owns.
        public string EmbeddingModel { get; set; } = string.Empty;
        public string EmbeddingVersion { get; set; } = string.Empty;
    }
}
