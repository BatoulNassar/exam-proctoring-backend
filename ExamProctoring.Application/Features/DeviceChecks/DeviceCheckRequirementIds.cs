using System;
using System.Collections.Generic;

namespace ExamProctoring.Application.Features.DeviceChecks
{
    /// The requirement identifiers the student desktop client may report.
    /// These values are part of the API contract, are matched case-sensitively,
    /// and are never localized.
    public static class DeviceCheckRequirementIds
    {
        public const string Camera = "CAMERA";
        public const string ScreenSize = "SCREEN_SIZE";
        public const string SingleDisplay = "SINGLE_DISPLAY";
        public const string Network = "NETWORK";
        public const string DiskSpace = "DISK_SPACE";
        public const string OperatingSystem = "OPERATING_SYSTEM";

        public static readonly IReadOnlySet<string> Supported = new HashSet<string>(StringComparer.Ordinal)
        {
            Camera,
            ScreenSize,
            SingleDisplay,
            Network,
            DiskSpace,
            OperatingSystem,
        };
    }

    /// The status values the client may report for a requirement.
    public static class DeviceCheckStatusValues
    {
        public const string Passed = "PASSED";
        public const string Warning = "WARNING";
        public const string Failed = "FAILED";

        public static readonly IReadOnlySet<string> Supported = new HashSet<string>(StringComparer.Ordinal)
        {
            Passed,
            Warning,
            Failed,
        };
    }
}
