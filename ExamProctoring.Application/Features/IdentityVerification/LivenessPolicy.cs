using System;

namespace ExamProctoring.Application.Features.IdentityVerification
{
    /// Server-side sanity checks on the client's liveness evidence (contract §3.4).
    ///
    /// The blink challenge runs entirely on the student's machine, so everything here is a
    /// CLAIM, not a fact. A patched client can send any numbers it likes. These checks cannot
    /// prove a live person was present - only a camera feed the backend never receives could
    /// do that - but they do reject payloads that could not have come from a real capture, and
    /// the contract asks for exactly that.
    ///
    /// Every limit is a named constant in this one class rather than a magic number inside a
    /// service or controller, so the values can be reviewed, tightened after calibration, or
    /// promoted to SystemSettings later without hunting through call sites. They are set
    /// conservatively: the cost of wrongly rejecting a real student minutes before an exam is
    /// far higher than the cost of accepting an implausible-but-not-impossible payload, which
    /// live proctoring still sees.
    public static class LivenessPolicy
    {
        /// Blinks the client must observe before it will submit. Contract §2.2 default.
        /// Reported to the client as policy.requiredBlinks and enforced here as well, because
        /// the client's own refusal is not a control the backend can rely on.
        public const int RequiredBlinks = 2;

        /// The client samples at roughly 10 fps, so a capture of N ms should yield about
        /// N/100 frames. Both bounds are generous: slow hardware legitimately drops frames,
        /// and a faster camera legitimately produces more.
        private const double ExpectedFramesPerMillisecond = 1d / 100d;
        private const double MinFrameRatio = 0.25;
        private const double MaxFrameRatio = 6.0;

        /// A blink cannot be observed across fewer than a handful of frames. The contract calls
        /// out "2 blinks in 3 frames" as the archetypal forged payload.
        private const int MinFramesPerBlink = 3;

        /// Shortest capture that could plausibly contain the required blinks.
        private const int MinDurationMs = 500;

        /// Longest capture accepted. Anything beyond this is a malformed or replayed payload
        /// rather than a verification attempt.
        private const int MaxDurationMs = 5 * 60 * 1000;

        /// Eye openness is a 0..1 ratio. A real blink drives the minimum toward 0 and the
        /// maximum toward 1; a payload where both sit mid-range never saw an eye close.
        /// "Near" is expressed as these two bounds so the qualitative wording in the contract
        /// becomes one reviewable number each, rather than a hidden judgement call.
        private const double MaxOpennessDuringBlink = 0.40;
        private const double MinOpennessWhenOpen = 0.60;

        /// Reasons are stable strings for the audit trail and logs. They are never returned to
        /// the student: a client that learns exactly which bound it failed can be tuned to
        /// satisfy the check while still forging the evidence.
        public static bool TryValidate(
            int blinkCount,
            int framesAnalysed,
            int durationMs,
            double minEyeOpenness,
            double maxEyeOpenness,
            out string? rejectionReason)
        {
            if (blinkCount < RequiredBlinks)
                return Reject($"blinkCount {blinkCount} is below the required {RequiredBlinks}", out rejectionReason);

            if (framesAnalysed <= 0)
                return Reject("framesAnalysed is not positive", out rejectionReason);

            if (durationMs < MinDurationMs || durationMs > MaxDurationMs)
                return Reject($"durationMs {durationMs} is outside {MinDurationMs}..{MaxDurationMs}", out rejectionReason);

            if (!double.IsFinite(minEyeOpenness) || !double.IsFinite(maxEyeOpenness))
                return Reject("eye openness values are not finite", out rejectionReason);

            if (minEyeOpenness < 0d || minEyeOpenness > 1d || maxEyeOpenness < 0d || maxEyeOpenness > 1d)
                return Reject("eye openness values are outside 0..1", out rejectionReason);

            if (minEyeOpenness >= maxEyeOpenness)
                return Reject("minEyeOpenness is not below maxEyeOpenness", out rejectionReason);

            if (minEyeOpenness > MaxOpennessDuringBlink)
                return Reject("minEyeOpenness is too high for an observed blink", out rejectionReason);

            if (maxEyeOpenness < MinOpennessWhenOpen)
                return Reject("maxEyeOpenness is too low for an open eye", out rejectionReason);

            if (framesAnalysed < blinkCount * MinFramesPerBlink)
                return Reject(
                    $"framesAnalysed {framesAnalysed} cannot contain {blinkCount} blink(s)", out rejectionReason);

            var expectedFrames = durationMs * ExpectedFramesPerMillisecond;

            if (framesAnalysed < expectedFrames * MinFrameRatio || framesAnalysed > expectedFrames * MaxFrameRatio)
                return Reject(
                    $"framesAnalysed {framesAnalysed} is implausible for durationMs {durationMs}", out rejectionReason);

            rejectionReason = null;
            return true;
        }

        private static bool Reject(string reason, out string? rejectionReason)
        {
            rejectionReason = reason;
            return false;
        }
    }
}
