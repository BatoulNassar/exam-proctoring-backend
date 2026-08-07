namespace ExamProctoring.Application.Features.DeviceChecks.DTOs
{
    /// One requirement result as reported by the student desktop client.
    public class DeviceCheckRequirementDto
    {
        /// One of DeviceCheckRequirementIds, uppercase, matched case-sensitively.
        public string? Id { get; set; }

        /// PASSED, WARNING or FAILED.
        public string? Status { get; set; }

        /// Optional short description, for example "1920x1080" or "48 ms".
        public string? Detail { get; set; }
    }
}
