using ExamProctoring.Application.Features.Streaming.DTOs;
using ExamProctoring.Application.Features.Streaming.Services;
using ExamProctoring.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

namespace ExamProctoring.API.Hubs
{
    public partial class MonitoringHub
    {
        public async Task<RequestStreamWatchResultDto> RequestStreamWatch(int studentSessionId)
        {
            if (IsStudentToken())
            {
                await RejectWatchAsync(studentSessionId, StreamWatchCodes.NotAuthorized,
                    "Student clients cannot request a stream watch.");
                throw new HubException("Student clients cannot request a stream watch");
            }

            var studentSession = await _studentSessionRepository.GetByIdAsync(studentSessionId);
            if (studentSession == null)
            {
                await RejectWatchAsync(studentSessionId, StreamWatchCodes.StudentNotInSession,
                    "Student session was not found.");
                throw new HubException("Student session was not found");
            }

            var examSessionId = studentSession.exam_session_id;

            if (!await CanAccessExamSessionAsync(examSessionId))
            {
                await RejectWatchAsync(studentSessionId, StreamWatchCodes.NotAuthorized,
                    "You are not authorised to watch this exam session.");
                throw new HubException("You are not authorised to watch this exam session");
            }

            if (!_presence.HasJoinedExamSession(Context.ConnectionId, examSessionId))
            {
                await RejectWatchAsync(studentSessionId, StreamWatchCodes.StudentNotInSession,
                    "Join the exam session hub group before requesting a watch.");
                throw new HubException("Join the exam session hub group before requesting a watch");
            }

            if (studentSession.finalised_at != null
                || studentSession.status is StudentSessionStatus.Submitted
                    or StudentSessionStatus.Terminated)
            {
                await RejectWatchAsync(studentSessionId, StreamWatchCodes.InvalidState,
                    "This attempt is no longer live for watching.");
                throw new HubException("This attempt is no longer live for watching");
            }

            if (!_presence.TryGetStudentConnectionId(studentSessionId, out var studentConnectionId))
            {
                await RejectWatchAsync(studentSessionId, StreamWatchCodes.StudentOffline,
                    "Student is not connected to the hub.");
                throw new HubException("Student is not connected to the hub");
            }

            if (!_watches.TryStart(
                    examSessionId,
                    studentSessionId,
                    Context.ConnectionId,
                    studentConnectionId,
                    _webRtc.MaxConcurrentWatchesPerSession,
                    out var entry,
                    out var rejectCode))
            {
                await RejectWatchAsync(
                    studentSessionId,
                    rejectCode ?? StreamWatchCodes.InvalidState,
                    rejectCode switch
                    {
                        StreamWatchCodes.WatchInProgress => "A watch is already in progress for this student.",
                        StreamWatchCodes.Capacity => "Concurrent watch capacity for this session was reached.",
                        StreamWatchCodes.StudentOffline => "Student is not connected to the hub.",
                        _ => "Unable to start stream watch.",
                    });
                throw new HubException("Unable to start stream watch");
            }

            var requested = new StreamWatchRequestedDto
            {
                WatchId = entry.WatchId,
                ExamSessionId = examSessionId,
                StudentSessionId = studentSessionId,
                RequestedAtUtc = DateTime.UtcNow,
            };

            await _streamNotifier.NotifyWatchRequestedAsync(
                studentSessionId,
                Context.ConnectionId,
                requested);

            _logger.LogInformation(
                "Stream watch {WatchId} started for studentSession {StudentSessionId} examSession {ExamSessionId}",
                entry.WatchId,
                studentSessionId,
                examSessionId);

            return new RequestStreamWatchResultDto { WatchId = entry.WatchId };
        }

        public async Task StreamOffer(StreamSdpDto payload)
        {
            EnsureSignalingPayload(payload.WatchId, payload.Sdp);
            if (!_watches.TryGet(payload.WatchId, out var watch))
                throw new HubException("Unknown watchId");

            if (!string.Equals(watch.StudentConnectionId, Context.ConnectionId, StringComparison.Ordinal))
                throw new HubException("Only the watched student may send the offer");

            if (!string.Equals(payload.SdpType, "offer", StringComparison.OrdinalIgnoreCase))
                throw new HubException("sdpType must be offer");

            await _streamNotifier.NotifyOfferAsync(watch.ProctorConnectionId, payload);
        }

        public async Task StreamAnswer(StreamSdpDto payload)
        {
            EnsureSignalingPayload(payload.WatchId, payload.Sdp);
            if (!_watches.TryGet(payload.WatchId, out var watch))
                throw new HubException("Unknown watchId");

            if (!string.Equals(watch.ProctorConnectionId, Context.ConnectionId, StringComparison.Ordinal))
                throw new HubException("Only the requesting proctor may send the answer");

            if (!string.Equals(payload.SdpType, "answer", StringComparison.OrdinalIgnoreCase))
                throw new HubException("sdpType must be answer");

            await _streamNotifier.NotifyAnswerAsync(watch.StudentConnectionId, payload);
        }

        public async Task IceCandidate(StreamIceCandidateDto payload)
        {
            if (payload.WatchId == Guid.Empty)
                throw new HubException("watchId is required");

            if (payload.Candidate != null
                && System.Text.Encoding.UTF8.GetByteCount(payload.Candidate) > MaxSignalingPayloadBytes)
            {
                throw new HubException("ICE candidate payload exceeds size limit");
            }

            if (!_watches.TryGet(payload.WatchId, out var watch))
                throw new HubException("Unknown watchId");

            string target;
            if (string.Equals(watch.ProctorConnectionId, Context.ConnectionId, StringComparison.Ordinal))
                target = watch.StudentConnectionId;
            else if (string.Equals(watch.StudentConnectionId, Context.ConnectionId, StringComparison.Ordinal))
                target = watch.ProctorConnectionId;
            else
                throw new HubException("Only watch peers may exchange ICE candidates");

            await _streamNotifier.NotifyIceCandidateAsync(target, payload);
        }

        public async Task EndStreamWatch(Guid watchId)
        {
            if (watchId == Guid.Empty)
                return;

            if (!_watches.TryGet(watchId, out var existing))
                return;

            var isProctor = string.Equals(
                existing.ProctorConnectionId,
                Context.ConnectionId,
                StringComparison.Ordinal);
            var isStudent = string.Equals(
                existing.StudentConnectionId,
                Context.ConnectionId,
                StringComparison.Ordinal);

            if (!isProctor && !isStudent)
                throw new HubException("Only watch peers may end the watch");

            if (!_watches.TryRemove(watchId, out var watch))
                return;

            var reason = isProctor
                ? StreamWatchCodes.ProctorEnded
                : StreamWatchCodes.StudentEnded;

            await _streamNotifier.NotifyWatchEndedAsync(
                watch.ProctorConnectionId,
                watch.StudentConnectionId,
                new StreamWatchEndedDto
                {
                    WatchId = watch.WatchId,
                    StudentSessionId = watch.StudentSessionId,
                    Reason = reason,
                    EndedAtUtc = DateTime.UtcNow,
                });

            _logger.LogInformation(
                "Stream watch {WatchId} ended ({Reason})",
                watch.WatchId,
                reason);
        }

        private async Task<bool> CanAccessExamSessionAsync(int examSessionId)
        {
            if (IsPrivilegedDashboardUser())
                return true;

            var proctorId = GetUserId();
            if (proctorId == null)
                return false;

            var assigned = await _proctorSessionRepository.GetSessionIdsByProctorAsync(proctorId.Value);
            return assigned.Contains(examSessionId);
        }

        private async Task RejectWatchAsync(int studentSessionId, string code, string message)
        {
            _logger.LogInformation(
                "Stream watch rejected for studentSession {StudentSessionId}: {Code}",
                studentSessionId,
                code);

            await _streamNotifier.NotifyWatchRejectedAsync(
                Context.ConnectionId,
                new StreamWatchRejectedDto
                {
                    StudentSessionId = studentSessionId,
                    Code = code,
                    Message = message,
                });
        }

        private static void EnsureSignalingPayload(Guid watchId, string? sdp)
        {
            if (watchId == Guid.Empty)
                throw new HubException("watchId is required");

            if (string.IsNullOrWhiteSpace(sdp))
                throw new HubException("sdp is required");

            if (System.Text.Encoding.UTF8.GetByteCount(sdp) > MaxSignalingPayloadBytes)
                throw new HubException("SDP payload exceeds size limit");
        }
    }
}
