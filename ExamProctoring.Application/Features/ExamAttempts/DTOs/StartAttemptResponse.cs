using System;
using System.Collections.Generic;

namespace ExamProctoring.Application.Features.ExamAttempts.DTOs
{
    /// Response body for Start/Resume. Identical shape for 201 (new) and 200 (resumed).
    public class StartAttemptResponse
    {
        /// Opaque attempt UUID. Required on every subsequent attempt-scoped call.
        public Guid AttemptId { get; set; }

        /// Integer ExamSession id, consistent with GET /sessions/eligibility and
        /// POST /device-checks, which already expose int session ids to this client.
        public int ExamSessionId { get; set; }

        /// The StudentSession id. Needed by the client to join its own SignalR group
        /// (MonitoringHub.JoinStudentSession takes this integer), which no other endpoint
        /// currently hands out.
        public int StudentSessionId { get; set; }

        /// IN_PROGRESS | SUBMITTED | TERMINATED | EXPIRED
        public string Status { get; set; } = string.Empty;

        public DateTime StartedAtUtc { get; set; }

        /// Absolute personal deadline. Immutable across resumes.
        public DateTime EndsAtUtc { get; set; }

        public DateTime ServerTimeUtc { get; set; }

        /// When the cohort's grace period ends, if one is running. Messaging only - EndsAtUtc
        /// remains this student's write cutoff.
        public DateTime? GraceEndsAtUtc { get; set; }

        public AttemptIdentityDto Identity { get; set; } = new();

        public MonitoringPolicyDto MonitoringPolicy { get; set; } = new();

        /// Previously persisted answers, so the client can restore its UI after a crash.
        /// Empty on a first start; populated on resume once answers exist.
        public List<SavedAnswerDto> SavedAnswers { get; set; } = new();

        /// Total questions materialised for this student, frozen at first start.
        public int QuestionCount { get; set; }
    }

    public class AttemptIdentityDto
    {
        /// FACE_MATCH | PROCTOR_OVERRIDE
        public string Method { get; set; } = string.Empty;

        public DateTime VerifiedAtUtc { get; set; }
    }

    public class MonitoringPolicyDto
    {
        public int GazeDeviationThresholdSeconds { get; set; }
        public bool AudioMonitoringEnabled { get; set; }
        public int AudioNoiseThresholdDb { get; set; }
        public int HeartbeatIntervalSeconds { get; set; }
        public int ConnectivityLostThresholdSeconds { get; set; }
    }

    /// One persisted answer, keyed by the per-attempt question public id.
    public class SavedAnswerDto
    {
        public Guid QuestionId { get; set; }
        public AnswerValueDto Value { get; set; } = new();
        public int DurationMs { get; set; }
        public DateTime SavedAtUtc { get; set; }
    }
}
