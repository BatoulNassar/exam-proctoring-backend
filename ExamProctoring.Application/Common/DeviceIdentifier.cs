using System;

namespace ExamProctoring.Application.Common
{
    /// Single definition of how a request's deviceId is reconciled with the authenticated
    /// token's device_id claim. Extracted from DeviceCheckService so Start Exam applies
    /// exactly the same rule instead of restating it.
    public static class DeviceIdentifier
    {
        /// Both values must be valid UUIDs and must match once normalized to the canonical
        /// "D" form. The resolved value always comes from the signed token claim, never from
        /// the request body.
        public static bool TryResolve(string? requestDeviceId, string? deviceIdClaim, out string deviceId)
        {
            deviceId = string.Empty;

            if (!Guid.TryParse(deviceIdClaim, out var claimDevice))
                return false;

            if (!Guid.TryParse(requestDeviceId, out var bodyDevice))
                return false;

            if (claimDevice != bodyDevice)
                return false;

            deviceId = Normalize(claimDevice);
            return true;
        }

        /// Canonical "D" form (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).
        public static string Normalize(Guid deviceId) => deviceId.ToString("D");

        /// Compares a canonical device id against a persisted binding. Both sides are parsed
        /// so a difference in casing or formatting is not mistaken for a different device.
        public static bool Matches(string? boundDeviceId, string? candidateDeviceId) =>
            Guid.TryParse(boundDeviceId, out var bound)
            && Guid.TryParse(candidateDeviceId, out var candidate)
            && bound == candidate;
    }
}
