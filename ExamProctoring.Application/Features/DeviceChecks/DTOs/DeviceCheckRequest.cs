using System.Collections.Generic;

namespace ExamProctoring.Application.Features.DeviceChecks.DTOs
{
    /// Pre-exam device-readiness report submitted by the Flutter Windows client.
    /// Every value here is client-measured and is stored as telemetry, not as
    /// verified fact about the machine.
    public class DeviceCheckRequest
    {
        /// ExamSession identifier; the backend uses int keys.
        public int? SessionId { get; set; }

        /// UUID of the installation; must match the authenticated token's device_id.
        public string? DeviceId { get; set; }

        /// Client clock reading, ISO-8601 with an explicit UTC designator.
        /// Bound as a string so a malformed value is reported through the project's
        /// ApiResponse envelope instead of framework ProblemDetails.
        public string? CheckedAtUtc { get; set; }

        /// The client's own proceed decision. Nullable so an omitted value is
        /// distinguishable from an explicit false; required by validation.
        public bool? CanProceed { get; set; }

        public List<DeviceCheckRequirementDto>? Requirements { get; set; }
    }
}
