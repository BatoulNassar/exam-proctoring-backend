using ExamProctoring.Domain.Enums;
using System;
using System.Security.Cryptography;
using System.Text;

namespace ExamProctoring.Application.Features.ExamAttempts.Services
{
    /// Semantic fingerprint of a submit request, for idempotency comparison. Pure.
    ///
    /// Only the target attempt and the reason are semantic. clientTimeUtc and clientMutationId are
    /// excluded on purpose: a client retrying a lost submit legitimately re-stamps its clock, and
    /// punishing that with a 409 would leave the student staring at an error after their exam had
    /// in fact been recorded.
    public static class SubmitRequestCanonicaliser
    {
        public static string Canonicalise(Guid attemptPublicId, AttemptFinalisationReason reason)
        {
            var builder = new StringBuilder();

            builder.Append("attempt=").Append(attemptPublicId.ToString("D").ToLowerInvariant()).Append('\n');
            builder.Append("reason=").Append(SubmitReasonMap.ToContract(reason)).Append('\n');

            return builder.ToString();
        }

        public static byte[] Hash(Guid attemptPublicId, AttemptFinalisationReason reason) =>
            SHA256.HashData(Encoding.UTF8.GetBytes(Canonicalise(attemptPublicId, reason)));
    }

    /// Translation between the contract's submit reasons and the persisted enum.
    /// ServerExpiry is intentionally absent from the client-facing direction: only the server can
    /// decide an attempt expired without a submit, so a client must not be able to claim it.
    public static class SubmitReasonMap
    {
        public const string StudentSubmit = "STUDENT_SUBMIT";
        public const string ClientTimerExpired = "CLIENT_TIMER_EXPIRED";
        public const string ProctorTerminated = "PROCTOR_TERMINATED";
        public const string ConnectivityRecovery = "CONNECTIVITY_RECOVERY";

        public static bool TryFromContract(string? value, out AttemptFinalisationReason reason)
        {
            switch (value?.Trim().ToUpperInvariant())
            {
                case StudentSubmit:
                    reason = AttemptFinalisationReason.StudentSubmit;
                    return true;
                case ClientTimerExpired:
                    reason = AttemptFinalisationReason.ClientTimerExpired;
                    return true;
                case ProctorTerminated:
                    reason = AttemptFinalisationReason.ProctorTerminated;
                    return true;
                case ConnectivityRecovery:
                    reason = AttemptFinalisationReason.ConnectivityRecovery;
                    return true;
                default:
                    reason = default;
                    return false;
            }
        }

        public static string ToContract(AttemptFinalisationReason reason) => reason switch
        {
            AttemptFinalisationReason.StudentSubmit => StudentSubmit,
            AttemptFinalisationReason.ClientTimerExpired => ClientTimerExpired,
            AttemptFinalisationReason.ProctorTerminated => ProctorTerminated,
            AttemptFinalisationReason.ConnectivityRecovery => ConnectivityRecovery,

            // Server expiry has no client-facing reason; it is only ever produced internally.
            AttemptFinalisationReason.ServerExpiry => "SERVER_EXPIRY",

            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unmapped submit reason."),
        };
    }
}
