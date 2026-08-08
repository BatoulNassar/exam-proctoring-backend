using System.Security.Claims;

namespace ExamProctoring.API.Common
{
    /// Reads the student desktop token's claims.
    ///
    /// Claim names and their shapes are an HTTP/authentication concern, so they belong at the API
    /// boundary - but they were being re-parsed inline in four different actions, which is how the
    /// same claim ends up validated three slightly different ways. One definition instead.
    ///
    /// This performs no authorization: the StudentOnly policy already guarantees a positive
    /// student_id before any action runs. These are defensive reads only.
    public static class ClaimsPrincipalExtensions
    {
        private const string StudentIdClaim = "student_id";
        private const string DeviceIdClaim = "device_id";

        public static bool TryGetStudentId(this ClaimsPrincipal user, out int studentId) =>
            int.TryParse(user.FindFirst(StudentIdClaim)?.Value, out studentId) && studentId > 0;

        /// Raw claim value. Whether it matches the request or the attempt's binding is a business
        /// decision and is made in the Application layer.
        public static string? GetDeviceId(this ClaimsPrincipal user) =>
            user.FindFirst(DeviceIdClaim)?.Value;
    }
}
