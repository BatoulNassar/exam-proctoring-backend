using ExamProctoring.Domain.Common;
using ExamProctoring.Domain.Enums;

namespace ExamProctoring.Domain.Entities
{
    /// One requirement result inside a DeviceCheck report.
    public class DeviceCheckRequirement : BaseEntity
    {
        public int device_check_id { get; set; }

        /// One of the supported requirement identifiers, stored in the contract's
        /// uppercase form.
        public string requirement_id { get; set; } = string.Empty;

        public DeviceCheckStatus status { get; set; }

        /// Short client-supplied description, sanitized and length-limited.
        public string? detail { get; set; }

        public DeviceCheck DeviceCheck { get; set; }
    }
}
