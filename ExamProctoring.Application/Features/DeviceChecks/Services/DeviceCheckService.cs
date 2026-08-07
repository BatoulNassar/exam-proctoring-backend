using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Application.Features.DeviceChecks.DTOs;
using ExamProctoring.Application.Features.DeviceChecks.Validators;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Features.DeviceChecks.Services
{
    public class DeviceCheckService : IDeviceCheckService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IStudentSessionRepository _studentSessionRepository;
        private readonly IDeviceCheckRepository _deviceCheckRepository;
        private readonly ILogger<DeviceCheckService> _logger;

        public DeviceCheckService(
            IStudentRepository studentRepository,
            IStudentSessionRepository studentSessionRepository,
            IDeviceCheckRepository deviceCheckRepository,
            ILogger<DeviceCheckService> logger)
        {
            _studentRepository = studentRepository;
            _studentSessionRepository = studentSessionRepository;
            _deviceCheckRepository = deviceCheckRepository;
            _logger = logger;
        }

        public async Task<DeviceCheckResult> RecordAsync(DeviceCheckRequest request, int studentId, string? deviceIdClaim)
        {
            // One server instant for the whole request; the client timestamp never drives anything.
            var receivedAtUtc = DateTime.UtcNow;

            // The 60-minute token outlives deactivation, so the student is revalidated here.
            // A soft-deleted student is hidden by the global filter and is deliberately
            // indistinguishable from one that never existed.
            var student = await _studentRepository.GetByIdAsync(studentId);
            if (student == null || !student.is_active)
            {
                _logger.LogWarning(
                    "Device check rejected: student {StudentId} is missing, deleted or inactive.", studentId);

                return DeviceCheckResult.AccountInactive;
            }

            if (!TryResolveDeviceId(request.DeviceId, deviceIdClaim, out var deviceId))
            {
                // Never log either UUID, only that the comparison failed.
                _logger.LogWarning(
                    "Device check rejected for student {StudentId}, session {SessionId}: deviceMatched=false.",
                    studentId, request.SessionId);

                return DeviceCheckResult.DeviceMismatch;
            }

            // Same visibility rules as Eligibility: not soft-deleted, not DRAFT, owned by this student.
            var assignment = await _studentSessionRepository
                .GetVisibleAssignmentAsync(studentId, request.SessionId!.Value);

            if (assignment == null)
            {
                _logger.LogWarning(
                    "Device check rejected: session {SessionId} is not a visible assignment for student {StudentId}.",
                    request.SessionId, studentId);

                return DeviceCheckResult.SessionNotFound;
            }

            // Validation guarantees the timestamp parses; the value is stored as reported.
            DeviceCheckRequestValidator.TryParseUtc(request.CheckedAtUtc, out var checkedAtUtc);

            var deviceCheck = new DeviceCheck
            {
                student_session_id = assignment.StudentSessionId,
                device_id = deviceId,
                checked_at_utc = checkedAtUtc,
                received_at_utc = receivedAtUtc,
                client_can_proceed = request.CanProceed!.Value,
                exam_session_status = assignment.ExamSessionStatus,
                created_at = receivedAtUtc,
            };

            foreach (var requirement in request.Requirements!)
            {
                deviceCheck.Requirements.Add(new DeviceCheckRequirement
                {
                    requirement_id = requirement.Id!,
                    status = MapStatus(requirement.Status!),
                    detail = NormalizeDetail(requirement.Detail),
                    created_at = receivedAtUtc,
                });
            }

            var deviceCheckId = await _deviceCheckRepository.AddAsync(deviceCheck);

            _logger.LogInformation(
                "Device check {DeviceCheckId} recorded for student {StudentId}, session {SessionId}: " +
                "{RequirementCount} requirement(s), clientCanProceed={ClientCanProceed}, sessionStatus={SessionStatus}.",
                deviceCheckId, studentId, request.SessionId, deviceCheck.Requirements.Count,
                deviceCheck.client_can_proceed, deviceCheck.exam_session_status);

            return DeviceCheckResult.Accepted;
        }

        /// Both values must be valid UUIDs and must match once normalized to the canonical
        /// "D" form. The stored value always comes from the signed token claim, never from
        /// the request body.
        private static bool TryResolveDeviceId(string? requestDeviceId, string? deviceIdClaim, out string deviceId)
        {
            deviceId = string.Empty;

            if (!Guid.TryParse(deviceIdClaim, out var claimDevice))
                return false;

            if (!Guid.TryParse(requestDeviceId, out var bodyDevice))
                return false;

            if (claimDevice != bodyDevice)
                return false;

            deviceId = claimDevice.ToString("D");
            return true;
        }

        private static DeviceCheckStatus MapStatus(string status) => status switch
        {
            DeviceCheckStatusValues.Passed => DeviceCheckStatus.Passed,
            DeviceCheckStatusValues.Warning => DeviceCheckStatus.Warning,
            _ => DeviceCheckStatus.Failed,
        };

        /// An empty or whitespace-only detail is stored as null rather than an empty string.
        private static string? NormalizeDetail(string? detail) =>
            string.IsNullOrWhiteSpace(detail) ? null : detail.Trim();
    }
}
