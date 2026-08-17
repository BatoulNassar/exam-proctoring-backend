using ExamProctoring.Application.Features.Alerts.DTOs;
using ExamProctoring.Domain.Enums;
using System.Collections.Generic;
using System.Linq;

namespace ExamProctoring.Application.Features.Alerts
{
    /// <summary>
    /// Single source of truth for the alert types the system can raise: their code
    /// (as stored in AlertEvent.alert_type), the label shown in the UI and the
    /// description shown under the alert name. Adding a type here makes it appear
    /// in the alerts filter without any frontend change.
    /// </summary>
    public static class AlertTypeCatalog
    {
        private static readonly List<AlertTypeDto> Types = new()
        {
            new AlertTypeDto { Code = "GazeDeviation",  Label = "Gaze Deviation",  Description = "Eyes off-screen beyond threshold" },
            new AlertTypeDto { Code = "FaceAbsence",    Label = "Face Absence",    Description = "No face detected in camera frame" },
            new AlertTypeDto { Code = "MultipleFaces",  Label = "Multiple Faces",  Description = "More than one face detected" },
            new AlertTypeDto { Code = "AppSwitch",      Label = "App Switch",      Description = "Switched to another application or window" },
            new AlertTypeDto { Code = "AudioThreshold", Label = "Audio Threshold", Description = "High noise level detected on microphone" },
            new AlertTypeDto { Code = "ShortcutAttempt", Label = "Shortcut Attempt", Description = "Blocked keyboard shortcut that can break kiosk lockdown" },
            new AlertTypeDto { Code = "KioskFailure",   Label = "Kiosk Failure",   Description = "Exam client failed to enter or maintain kiosk mode" },
            new AlertTypeDto { Code = "KioskEmergencyExit", Label = "Kiosk Emergency Exit", Description = "Student left kiosk mode using the emergency Escape key" },
        };

        /// <summary>
        /// Default severity per type. Used when the monitoring client reports an
        /// event and the backend decides how serious the resulting alert is — the
        /// client never gets to choose its own severity.
        /// </summary>
        private static readonly Dictionary<string, AlertSeverity> Severities = new()
        {
            { "GazeDeviation",  AlertSeverity.Warning },
            { "FaceAbsence",    AlertSeverity.Critical },
            { "MultipleFaces",  AlertSeverity.Critical },
            { "AppSwitch",      AlertSeverity.Warning },
            { "AudioThreshold", AlertSeverity.Warning },
            { "ShortcutAttempt", AlertSeverity.Warning },
            { "KioskFailure",   AlertSeverity.Critical },
            { "KioskEmergencyExit", AlertSeverity.Critical },
        };

        public static IReadOnlyList<AlertTypeDto> All => Types;

        public static bool IsKnown(string code) =>
            Types.Any(t => t.Code == code);

        public static AlertSeverity GetSeverity(string code) =>
            Severities.TryGetValue(code ?? "", out var severity) ? severity : AlertSeverity.Warning;

        public static string GetDescription(string code) =>
            Types.FirstOrDefault(t => t.Code == code)?.Description ?? code;
    }
}
