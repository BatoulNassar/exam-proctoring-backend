using System;

namespace ExamProctoring.Application.Features.Eligibility.DTOs
{
    /// Payload carried inside ApiResponse&lt;T&gt;.Data when eligibility is resolved.
    public class EligibilityResponse
    {
        public bool IsEligible { get; set; }

        /// Null when the student may start; otherwise one of EligibilityReasonCodes.
        public string? ReasonCode { get; set; }

        /// The single UTC instant captured at the start of the request and used for
        /// every comparison in it.
        public DateTime ServerTimeUtc { get; set; }

        /// Null only when the student has no visible (non-draft) assignment.
        public EligibleSessionDto? Session { get; set; }
    }
}
