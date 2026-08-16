using ExamProctoring.Application.Features.Alerts.DTOs;
using ExamProctoring.Application.Features.Monitoring.DTOs;

namespace ExamProctoring.Application.Common.Interfaces
{
    /// <summary>
    /// Real-time fan-out for the monitoring screens. Two audiences:
    /// proctors watching an exam session, and the single student a message is for.
    /// </summary>
    public interface IMonitoringNotifier
    {
        // ===== To the proctors watching an exam session =====

        Task NotifyAlertUpdatedAsync(int sessionId, int alertId, string newStatus, string? actionTaken);

        /// <summary>A new alert was raised. Drives the live alert feed and the card badges.</summary>
        Task NotifyAlertCreatedAsync(int sessionId, AlertEventDto alert);

        /// <summary>A student's state changed (logged in, disconnected, terminated, submitted, pipeline).</summary>
        Task NotifyStudentStatusChangedAsync(int examSessionId, StudentStatusChangedDto status);

        // ===== To the one student sitting the exam =====

        /// <summary>Delivers a proctor's warning to the student's own screen.</summary>
        Task NotifyStudentWarnedAsync(int studentSessionId, string message, int warningNumber, int maxWarnings);

        /// <summary>Tells the student their attempt has been ended, and why.</summary>
        Task NotifyStudentSessionTerminatedAsync(int studentSessionId, string reason);
    }
}
