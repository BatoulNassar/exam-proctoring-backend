using System;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    /// How identity was established for an attempt.
    /// These values are part of the student API contract.
    public static class IdentityVerificationMethods
    {
        public const string FaceMatch = "FACE_MATCH";
        public const string ProctorOverride = "PROCTOR_OVERRIDE";
    }

    /// Outcome of the identity gate for one attempt.
    public sealed class IdentityGateResult
    {
        public bool IsVerified { get; init; }

        /// <see cref="IdentityVerificationMethods"/> value, or null when not verified.
        public string? Method { get; init; }

        public DateTime? VerifiedAtUtc { get; init; }

        public static IdentityGateResult NotVerified() => new() { IsVerified = false };

        public static IdentityGateResult Verified(string method, DateTime verifiedAtUtc) =>
            new() { IsVerified = true, Method = method, VerifiedAtUtc = verifiedAtUtc };
    }

    /// The single seam between Start Exam and Identity Verification.
    ///
    /// Start Exam must never reason about face embeddings, similarity scores or liveness - it
    /// only asks whether identity is settled for this attempt and by which method. The
    /// Identity Verification feature is being built separately; the current implementation
    /// reads the existing StudentSession verification state and is intended to be replaced
    /// wholesale without touching any consumer.
    public interface IIdentityGate
    {
        /// <param name="studentSessionId">The attempt being started or resumed.</param>
        Task<IdentityGateResult> GetForAttemptAsync(int studentSessionId);
    }
}
