using RifeZPhoneBridge.Core.Audio;

namespace RifeZPhoneBridge.App;

public sealed class BridgeMetricsService
{
    private readonly object _sync = new();

    private DateTimeOffset? _sessionStartedUtc;
    private int _reconnectCount;
    private int _faultCount;

    private long _framesSent;
    private long _bytesSent;

    private double? _lastInitializeDurationMs;
    private double? _lastStartDurationMs;

    private string? _lastError;

    private readonly Queue<SampleTick> _sampleTicks = new();
    private const double HistoryWindowSeconds = 60.0;

    private readonly List<SessionEventSnapshot> _sessionEvents = new();
    private const int MaxSessionEvents = 30;

    private DateTimeOffset? _startupDiscoveringUtc;
    private DateTimeOffset? _startupReceiverSelectedUtc;
    private DateTimeOffset? _startupControlConnectedUtc;
    private DateTimeOffset? _startupStreamConfiguredUtc;
    private DateTimeOffset? _startupStreamingUtc;

    private sealed record SampleTick(
        DateTimeOffset TimestampUtc,
        long FramesSent,
        long BytesSent
    );

    public void MarkStreamingStarted()
    {
        lock (_sync)
        {
            _sessionStartedUtc = DateTimeOffset.UtcNow;
            _framesSent = 0;
            _bytesSent = 0;
            _sampleTicks.Clear();
            _sampleTicks.Enqueue(new SampleTick(_sessionStartedUtc.Value, 0, 0));
        }
    }

    public void MarkStreamingStopped()
    {
        lock (_sync)
        {
            _sessionStartedUtc = null;
            _sampleTicks.Clear();
        }
    }

    public void IncrementReconnect()
    {
        lock (_sync)
        {
            _reconnectCount++;
            AddSessionEvent_NoLock("Reconnect", null);
        }
    }

    public void IncrementFault(string? error)
    {
        lock (_sync)
        {
            _faultCount++;
            _lastError = error;
            AddSessionEvent_NoLock("Faulted", error);
        }
    }

    public void SetLastInitializeDuration(TimeSpan duration)
    {
        lock (_sync)
        {
            _lastInitializeDurationMs = duration.TotalMilliseconds;
        }
    }

    public void SetLastStartDuration(TimeSpan duration)
    {
        lock (_sync)
        {
            _lastStartDurationMs = duration.TotalMilliseconds;
        }
    }

    public void AddTelemetry(AudioSendTelemetrySample sample)
    {
        lock (_sync)
        {
            _framesSent++;
            _bytesSent += sample.PayloadBytes;

            var now = DateTimeOffset.UtcNow;

            bool shouldAppend =
                _sampleTicks.Count == 0 ||
                (now - _sampleTicks.Last().TimestampUtc).TotalMilliseconds >= 250;

            if (shouldAppend)
            {
                _sampleTicks.Enqueue(new SampleTick(now, _framesSent, _bytesSent));
                TrimHistory(now);
            }
        }
    }

    public void RecordStateTransition(string stateName, string? details = null)
    {
        lock (_sync)
        {
            var now = DateTimeOffset.UtcNow;
            AddSessionEvent_NoLock(stateName, details);

            switch (stateName)
            {
                case "Discovering":
                    _startupDiscoveringUtc = now;
                    _startupReceiverSelectedUtc = null;
                    _startupControlConnectedUtc = null;
                    _startupStreamConfiguredUtc = null;
                    _startupStreamingUtc = null;
                    break;

                case "ReceiverSelected":
                    _startupReceiverSelectedUtc = now;
                    break;

                case "ControlConnected":
                    _startupControlConnectedUtc = now;
                    break;

                case "StreamConfigured":
                    _startupStreamConfiguredUtc = now;
                    break;

                case "Streaming":
                    _startupStreamingUtc = now;
                    break;
            }
        }
    }

    public BridgeMetricsSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            var uptime = _sessionStartedUtc.HasValue
                ? DateTimeOffset.UtcNow - _sessionStartedUtc.Value
                : TimeSpan.Zero;

            double fps = 0;
            double kbps = 0;

            if (uptime.TotalSeconds > 0.001)
            {
                fps = _framesSent / uptime.TotalSeconds;
                kbps = (_bytesSent / 1024.0) / uptime.TotalSeconds;
            }

            var now = DateTimeOffset.UtcNow;
            TrimHistory(now);

            var throughputHistory = BuildThroughputHistory(now);
            var fpsHistory = BuildFpsHistory(now);

            return new BridgeMetricsSnapshot
            {
                SessionStartedUtc = _sessionStartedUtc,
                Uptime = uptime,
                ReconnectCount = _reconnectCount,
                FaultCount = _faultCount,
                FramesSent = _framesSent,
                BytesSent = _bytesSent,
                FramesPerSecond = fps,
                KilobytesPerSecond = kbps,
                LastInitializeDurationMs = _lastInitializeDurationMs,
                LastStartDurationMs = _lastStartDurationMs,
                LastError = _lastError,
                ThroughputHistory = throughputHistory,
                FpsHistory = fpsHistory,
                SessionEvents = _sessionEvents.ToArray(),
                DiscoveryDurationMs = DiffMs(_startupDiscoveringUtc, _startupReceiverSelectedUtc),
                ReceiverSelectionDurationMs = DiffMs(_startupReceiverSelectedUtc, _startupControlConnectedUtc),
                ControlConnectDurationMs = DiffMs(_startupControlConnectedUtc, _startupStreamConfiguredUtc),
                StreamConfigureDurationMs = DiffMs(_startupStreamConfiguredUtc, _startupStreamingUtc),
                StreamingTransitionDurationMs = DiffMs(_startupStreamConfiguredUtc, _startupStreamingUtc),
                TotalBringUpDurationMs = DiffMs(_startupDiscoveringUtc, _startupStreamingUtc)
            };
        }
    }

    private static double? DiffMs(DateTimeOffset? a, DateTimeOffset? b)
    {
        if (!a.HasValue || !b.HasValue)
            return null;

        return (b.Value - a.Value).TotalMilliseconds;
    }

    private void AddSessionEvent_NoLock(string eventName, string? details)
    {
        _sessionEvents.Add(new SessionEventSnapshot(DateTimeOffset.UtcNow, eventName, details));

        if (_sessionEvents.Count > MaxSessionEvents)
        {
            _sessionEvents.RemoveAt(0);
        }
    }

    private void TrimHistory(DateTimeOffset now)
    {
        while (_sampleTicks.Count > 0 &&
               (now - _sampleTicks.Peek().TimestampUtc).TotalSeconds > HistoryWindowSeconds)
        {
            _sampleTicks.Dequeue();
        }
    }

    private IReadOnlyList<MetricPoint> BuildThroughputHistory(DateTimeOffset now)
    {
        var list = new List<MetricPoint>();
        if (_sampleTicks.Count < 2)
            return list;

        SampleTick? prev = null;
        foreach (var tick in _sampleTicks)
        {
            if (prev is not null)
            {
                double dt = (tick.TimestampUtc - prev.TimestampUtc).TotalSeconds;
                if (dt > 0.0001)
                {
                    double kbps = ((tick.BytesSent - prev.BytesSent) / 1024.0) / dt;
                    double secondsAgo = (now - tick.TimestampUtc).TotalSeconds;
                    list.Add(new MetricPoint(secondsAgo, kbps));
                }
            }

            prev = tick;
        }

        return list;
    }

    private IReadOnlyList<MetricPoint> BuildFpsHistory(DateTimeOffset now)
    {
        var list = new List<MetricPoint>();
        if (_sampleTicks.Count < 2)
            return list;

        SampleTick? prev = null;
        foreach (var tick in _sampleTicks)
        {
            if (prev is not null)
            {
                double dt = (tick.TimestampUtc - prev.TimestampUtc).TotalSeconds;
                if (dt > 0.0001)
                {
                    double fps = (tick.FramesSent - prev.FramesSent) / dt;
                    double secondsAgo = (now - tick.TimestampUtc).TotalSeconds;
                    list.Add(new MetricPoint(secondsAgo, fps));
                }
            }

            prev = tick;
        }

        return list;
    }
}