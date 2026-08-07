namespace ExamProctoring.Application.Features.DeviceChecks.DTOs
{
    /// Outcome of recording a device check. The controller maps this to an HTTP
    /// status and a stable error code; success carries no response payload.
    public enum DeviceCheckResult
    {
        /// Stored. The controller returns 202 with an empty body.
        Accepted,

        /// The token is valid but the student is missing, soft-deleted or inactive.
        AccountInactive,

        /// The request device does not match the authenticated token's device_id,
        /// or the token carries no usable device_id.
        DeviceMismatch,

        /// The session does not exist, is not assigned to this student, is soft-deleted
        /// or is still DRAFT. Reported identically in every case.
        SessionNotFound,
    }
}
