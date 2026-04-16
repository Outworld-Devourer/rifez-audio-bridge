namespace RifeZPhoneBridge.App;

public sealed class BridgeMetricsSnapshot
{
    public DateTimeOffset? SessionStartedUtc { get; init; }
    public TimeSpan Uptime { get; init; }

    public int ReconnectCount { get; init; }
    public int FaultCount { get; init; }

    public long FramesSent { get; init; }
    public long BytesSent { get; init; }

    public double FramesPerSecond { get; init; }
    public double KilobytesPerSecond { get; init; }

    public double? LastInitializeDurationMs { get; init; }
    public double? LastStartDurationMs { get; init; }

    public string? LastError { get; init; }

    public IReadOnlyList<MetricPoint> ThroughputHistory { get; init; } = Array.Empty<MetricPoint>();
    public IReadOnlyList<MetricPoint> FpsHistory { get; init; } = Array.Empty<MetricPoint>();

    public IReadOnlyList<SessionEventSnapshot> SessionEvents { get; init; } = Array.Empty<SessionEventSnapshot>();

    public double? DiscoveryDurationMs { get; init; }
    public double? ReceiverSelectionDurationMs { get; init; }
    public double? ControlConnectDurationMs { get; init; }
    public double? StreamConfigureDurationMs { get; init; }
    public double? StreamingTransitionDurationMs { get; init; }
    public double? TotalBringUpDurationMs { get; init; }
}

public readonly record struct MetricPoint(
    double SecondsAgo,
    double Value
);

public readonly record struct SessionEventSnapshot(
    DateTimeOffset TimestampUtc,
    string EventName,
    string? Details
);