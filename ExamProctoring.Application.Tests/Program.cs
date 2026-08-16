using ExamProctoring.Application.Common.Settings;
using ExamProctoring.Application.Features.Alerts;
using ExamProctoring.Application.Features.Monitoring.DTOs;
using ExamProctoring.Application.Features.Monitoring.Services;
using ExamProctoring.Application.Features.Streaming.Services;
using ExamProctoring.Domain.Entities;
using ExamProctoring.Domain.Enums;
using Microsoft.Extensions.Options;

namespace ExamProctoring.Application.Tests;

/// Lightweight runner so registry/ICE/monitor tests can execute without NuGet Test SDK
/// when the feed is unavailable. Same assertions as the planned xUnit cases.
internal static class Program
{
    private static int _failed;

    private static int Main()
    {
        Run("TryStart_SecondWatchForSameStudent_ReturnsWatchInProgress",
            InMemoryStreamWatchRegistryTests.TryStart_SecondWatchForSameStudent_ReturnsWatchInProgress);
        Run("TryStart_WhenSessionAtCapacity_ReturnsCapacity",
            InMemoryStreamWatchRegistryTests.TryStart_WhenSessionAtCapacity_ReturnsCapacity);
        Run("RemoveAllForConnection_RemovesProctorWatches",
            InMemoryStreamWatchRegistryTests.RemoveAllForConnection_RemovesProctorWatches);
        Run("RemoveAllForConnection_RemovesStudentWatches",
            InMemoryStreamWatchRegistryTests.RemoveAllForConnection_RemovesStudentWatches);
        Run("TryRemove_IsIdempotentAfterFirstSuccess",
            InMemoryStreamWatchRegistryTests.TryRemove_IsIdempotentAfterFirstSuccess);
        Run("GetIceServers_WhenStunEmpty_ReturnsNull",
            IceServersServiceTests.GetIceServers_WhenStunEmpty_ReturnsNull);
        Run("GetIceServers_WhenStunOnly_ReturnsServersWithoutTurnCreds",
            IceServersServiceTests.GetIceServers_WhenStunOnly_ReturnsServersWithoutTurnCreds);
        Run("GetIceServers_WhenTurnConfigured_IncludesTurnEntry",
            IceServersServiceTests.GetIceServers_WhenTurnConfigured_IncludesTurnEntry);

        Run("Heartbeat_UpdatesLastBeat_AndDetectsPipelineChange",
            StudentHubPresenceTests.Heartbeat_UpdatesLastBeat_AndDetectsPipelineChange);
        Run("Heartbeat_RejectsWrongConnection",
            StudentHubPresenceTests.Heartbeat_RejectsWrongConnection);
        Run("PipelineStatuses_OnlyAllowsKnownValues",
            PipelineStatusTests.OnlyAllowsKnownValues);
        Run("MapToAlertDto_IncludesStudentSessionAndSnapshot",
            AlertMappingTests.MapToAlertDto_IncludesStudentSessionAndSnapshot);
        Run("AlertTypeCatalog_FaceAbsence_IsCritical",
            AlertCatalogTests.FaceAbsence_IsCritical);
        Run("MaxSnapshotDecodedBytes_Is400KiB",
            MonitoringConstantsTests.MaxSnapshotDecodedBytes_Is400KiB);

        Console.WriteLine(_failed == 0
            ? "Passed! 14 tests."
            : $"Failed! {_failed} test(s).");
        return _failed == 0 ? 0 : 1;
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            Console.WriteLine($"  ✓ {name}");
        }
        catch (Exception ex)
        {
            _failed++;
            Console.WriteLine($"  ✗ {name}");
            Console.WriteLine($"    {ex.Message}");
        }
    }
}

internal static class Assert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition)
            throw new Exception(message ?? "Expected true.");
    }

    public static void False(bool condition, string? message = null)
    {
        if (condition)
            throw new Exception(message ?? "Expected false.");
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected: {expected}, Actual: {actual}");
    }

    public static void NotNull(object? value)
    {
        if (value is null)
            throw new Exception("Expected non-null.");
    }

    public static void Null(object? value)
    {
        if (value is not null)
            throw new Exception("Expected null.");
    }

    public static void Single<T>(IEnumerable<T> source)
    {
        if (source.Count() != 1)
            throw new Exception("Expected single element.");
    }

    public static void Contains<T>(IEnumerable<T> source, Func<T, bool> predicate)
    {
        if (!source.Any(predicate))
            throw new Exception("Expected matching element.");
    }
}

internal static class InMemoryStreamWatchRegistryTests
{
    public static void TryStart_SecondWatchForSameStudent_ReturnsWatchInProgress()
    {
        var sut = new InMemoryStreamWatchRegistry();
        Assert.True(sut.TryStart(1, 10, "proctor-a", "student-a", 10, out var first, out _));
        Assert.False(sut.TryStart(1, 10, "proctor-b", "student-a", 10, out _, out var reject));
        Assert.Equal(StreamWatchCodes.WatchInProgress, reject);
        Assert.True(sut.TryGet(first.WatchId, out _));
    }

    public static void TryStart_WhenSessionAtCapacity_ReturnsCapacity()
    {
        var sut = new InMemoryStreamWatchRegistry();
        const int max = 10;
        for (var i = 0; i < max; i++)
        {
            Assert.True(sut.TryStart(7, 100 + i, $"proctor-{i}", $"student-{i}", max, out _, out _));
        }

        Assert.False(sut.TryStart(7, 999, "proctor-x", "student-x", max, out _, out var reject));
        Assert.Equal(StreamWatchCodes.Capacity, reject);
    }

    public static void RemoveAllForConnection_RemovesProctorWatches()
    {
        var sut = new InMemoryStreamWatchRegistry();
        Assert.True(sut.TryStart(1, 10, "proctor-a", "student-a", 10, out var a, out _));
        Assert.True(sut.TryStart(1, 11, "proctor-a", "student-b", 10, out var b, out _));
        Assert.True(sut.TryStart(1, 12, "proctor-other", "student-c", 10, out var c, out _));

        var removed = sut.RemoveAllForConnection("proctor-a");

        Assert.Equal(2, removed.Count);
        Assert.Contains(removed, e => e.WatchId == a.WatchId);
        Assert.Contains(removed, e => e.WatchId == b.WatchId);
        Assert.False(sut.TryGet(a.WatchId, out _));
        Assert.False(sut.TryGet(b.WatchId, out _));
        Assert.True(sut.TryGet(c.WatchId, out _));
    }

    public static void RemoveAllForConnection_RemovesStudentWatches()
    {
        var sut = new InMemoryStreamWatchRegistry();
        Assert.True(sut.TryStart(1, 10, "proctor-a", "student-a", 10, out var entry, out _));

        var removed = sut.RemoveAllForConnection("student-a");

        Assert.Single(removed);
        Assert.Equal(entry.WatchId, removed[0].WatchId);
        Assert.False(sut.TryGet(entry.WatchId, out _));
    }

    public static void TryRemove_IsIdempotentAfterFirstSuccess()
    {
        var sut = new InMemoryStreamWatchRegistry();
        Assert.True(sut.TryStart(1, 10, "proctor-a", "student-a", 10, out var entry, out _));
        Assert.True(sut.TryRemove(entry.WatchId, out _));
        Assert.False(sut.TryRemove(entry.WatchId, out _));
    }
}

internal static class IceServersServiceTests
{
    public static void GetIceServers_WhenStunEmpty_ReturnsNull()
    {
        var sut = new IceServersService(Options.Create(new WebRtcSettings
        {
            StunUrls = Array.Empty<string>(),
        }));
        Assert.Null(sut.GetIceServers());
    }

    public static void GetIceServers_WhenStunOnly_ReturnsServersWithoutTurnCreds()
    {
        var sut = new IceServersService(Options.Create(new WebRtcSettings
        {
            StunUrls = new[] { "stun:stun.l.google.com:19302" },
            TurnUrls = Array.Empty<string>(),
            IceCredentialTtlMinutes = 5,
        }));

        var result = sut.GetIceServers();
        Assert.NotNull(result);
        Assert.Equal(1, result!.IceServers.Count);
        Assert.Equal("stun:stun.l.google.com:19302", result.IceServers[0].Urls[0]);
        Assert.Null(result.IceServers[0].Username);
        Assert.NotNull(result.ExpiresAtUtc);
        Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
    }

    public static void GetIceServers_WhenTurnConfigured_IncludesTurnEntry()
    {
        var sut = new IceServersService(Options.Create(new WebRtcSettings
        {
            StunUrls = new[] { "stun:stun.example:3478" },
            TurnUrls = new[] { "turn:turn.example:3478" },
            TurnUsername = "user",
            TurnCredential = "secret",
            IceCredentialTtlMinutes = null,
        }));

        var result = sut.GetIceServers();
        Assert.NotNull(result);
        Assert.Equal(2, result!.IceServers.Count);
        Assert.Equal("user", result.IceServers[1].Username);
        Assert.Equal("secret", result.IceServers[1].Credential);
        Assert.Null(result.ExpiresAtUtc);
    }
}

internal static class StudentHubPresenceTests
{
    public static void Heartbeat_UpdatesLastBeat_AndDetectsPipelineChange()
    {
        var sut = new InMemoryStudentHubPresence();
        sut.SetStudentConnected(67, 43, "conn-1");

        var t1 = DateTime.UtcNow;
        Assert.True(sut.TryRecordHeartbeat(43, "conn-1", PipelineStatuses.Ok, t1, out var changed1, out var e1));
        Assert.True(changed1);
        Assert.Equal(PipelineStatuses.Ok, e1!.PipelineStatus);
        Assert.Equal(t1, e1.LastHeartbeatAtUtc);

        var t2 = t1.AddSeconds(10);
        Assert.True(sut.TryRecordHeartbeat(43, "conn-1", PipelineStatuses.Ok, t2, out var changed2, out var e2));
        Assert.False(changed2);
        Assert.Equal(t2, e2!.LastHeartbeatAtUtc);

        var t3 = t2.AddSeconds(10);
        Assert.True(sut.TryRecordHeartbeat(
            43, "conn-1", PipelineStatuses.CameraDown, t3, out var changed3, out var e3));
        Assert.True(changed3);
        Assert.Equal(PipelineStatuses.CameraDown, e3!.PipelineStatus);
    }

    public static void Heartbeat_RejectsWrongConnection()
    {
        var sut = new InMemoryStudentHubPresence();
        sut.SetStudentConnected(67, 43, "conn-1");
        Assert.False(sut.TryRecordHeartbeat(
            43, "other", PipelineStatuses.Ok, DateTime.UtcNow, out _, out _));
    }
}

internal static class PipelineStatusTests
{
    public static void OnlyAllowsKnownValues()
    {
        Assert.True(PipelineStatuses.IsKnown("OK"));
        Assert.True(PipelineStatuses.IsKnown("CAMERA_DOWN"));
        Assert.True(PipelineStatuses.IsKnown("ENGINE_DEGRADED"));
        Assert.False(PipelineStatuses.IsKnown("BAD"));
        Assert.False(PipelineStatuses.IsKnown(null));
    }
}

internal static class AlertMappingTests
{
    public static void MapToAlertDto_IncludesStudentSessionAndSnapshot()
    {
        var session = new StudentSession
        {
            id = 43,
            student_id = 12,
            exam_session_id = 67,
            Student = new Student
            {
                first_name = "Manar",
                last_name = "Jarkas",
                university_number = "VU-2024-002",
            },
            ExamSession = new ExamSession { title = "Algorithms Midterm" },
        };

        var alert = new AlertEvent
        {
            id = 1204,
            student_session_id = 43,
            alert_type = "FaceAbsence",
            severity = AlertSeverity.Critical,
            status = AlertStatus.Open,
            triggered_at = DateTime.UtcNow,
            snapshot_url = "https://cdn.example/snap.jpg",
        };

        var dto = MonitoringService.MapToAlertDto(alert, session);
        Assert.Equal(43, dto.StudentSessionId);
        Assert.Equal("https://cdn.example/snap.jpg", dto.SnapshotUrl);
        Assert.Equal("FaceAbsence", dto.AlertType);
        Assert.Equal("Critical", dto.Severity);
        Assert.Equal(67, dto.SessionId);
    }
}

internal static class AlertCatalogTests
{
    public static void FaceAbsence_IsCritical()
    {
        Assert.True(AlertTypeCatalog.IsKnown("FaceAbsence"));
        Assert.Equal(AlertSeverity.Critical, AlertTypeCatalog.GetSeverity("FaceAbsence"));
    }
}

internal static class MonitoringConstantsTests
{
    public static void MaxSnapshotDecodedBytes_Is400KiB()
    {
        Assert.Equal(400 * 1024, MonitoringService.MaxSnapshotDecodedBytes);
    }
}
