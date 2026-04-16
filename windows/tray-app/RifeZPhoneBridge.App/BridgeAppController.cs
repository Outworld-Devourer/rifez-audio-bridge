using RifeZPhoneBridge.Core.Audio;
using RifeZPhoneBridge.Core.Discovery;
using RifeZPhoneBridge.Host.Abstractions;
using RifeZPhoneBridge.Host.Models;
using RifeZPhoneBridge.Host.Services;
using System.Threading;

namespace RifeZPhoneBridge.App;

public sealed class BridgeAppController : IAsyncDisposable
{
    private readonly AppSettingsService _settingsService;
    private readonly BridgeAppSettings _settings;
    private readonly SimpleFileLogger _logger;
    private readonly BridgeMetricsService _metrics;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);

    private ReceiverSessionManager? _receiverSessionManager;
    private AudioStreamingCoordinator? _audioStreamingCoordinator;
    private IAudioInputProviderFactory? _inputProviderFactory;
    private BridgeHost? _bridgeHost;
    private BridgeCommandService? _commands;

    public event Action<BridgeRuntimeState>? StateChanged;

    public BridgeAppController(
        AppSettingsService settingsService,
        BridgeAppSettings settings,
        SimpleFileLogger logger,
        BridgeMetricsService metrics)
    {
        _settingsService = settingsService;
        _settings = settings;
        _logger = logger;
        _metrics = metrics;
    }

    public BridgeStatusSnapshot GetStatus()
    {
        EnsureCreated();
        return _commands!.GetStatus();
    }

    public BridgeMetricsSnapshot GetMetrics()
    {
        return _metrics.GetSnapshot();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            EnsureCreated();
            _logger.Info("Initialize requested.");

            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await _commands!.InitializeAsync(cancellationToken);
                sw.Stop();
                _metrics.SetLastInitializeDuration(sw.Elapsed);
            }
            catch (InvalidOperationException ex) when (IsReceiverUnavailable(ex))
            {
                sw.Stop();
                _metrics.SetLastInitializeDuration(sw.Elapsed);
                _logger.Info($"Initialize deferred: {ex.Message}");
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            EnsureCreated();
            _logger.Info("Start requested.");

            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await _commands!.StartAsync(cancellationToken);
                sw.Stop();
                _metrics.SetLastStartDuration(sw.Elapsed);
            }
            catch (InvalidOperationException ex) when (IsReceiverUnavailable(ex))
            {
                sw.Stop();
                _metrics.SetLastStartDuration(sw.Elapsed);
                _logger.Info($"Start deferred: {ex.Message}");
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            if (_commands is null)
                return;

            _logger.Info("Stop requested.");
            await _commands.StopAsync(cancellationToken);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            if (_commands is null)
                return;

            _logger.Info("Shutdown requested.");
            await _commands.ShutdownAsync(cancellationToken);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task ReconnectAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            EnsureCreated();
            _logger.Info("Reconnect requested.");
            _metrics.IncrementReconnect();

            if (_commands is not null)
            {
                await _commands.ShutdownAsync(cancellationToken);
            }

            try
            {
                if (_settings.AutoStartStreaming)
                {
                    await _commands!.StartAsync(cancellationToken);
                }
                else
                {
                    await _commands!.InitializeAsync(cancellationToken);
                }
            }
            catch (InvalidOperationException ex) when (IsReceiverUnavailable(ex))
            {
                _logger.Info($"Reconnect deferred: {ex.Message}");
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private void EnsureCreated()
    {
        if (_bridgeHost is not null)
            return;

        var options = new BridgeHostOptions
        {
            ClientName = _settings.ClientName,
            ManualHost = _settings.ManualHost,
            ManualPort = _settings.ManualPort,
            LastReceiverHost = _settings.LastReceiverHost,
            LastReceiverPort = _settings.LastReceiverPort,
            FlatBufferHelloPort = _settings.FlatBufferHelloPort,
            AudioPcmPort = _settings.AudioPcmPort,
            SampleRate = _settings.SampleRate,
            Channels = _settings.Channels,
            FrameSamples = _settings.FrameSamples,
            StartupBurstFrames = _settings.StartupBurstFrames,
            InputKind = _settings.InputKind,
            InputSourcePath = _settings.InputSourcePath
        };

        var discovery = new ZeroconfReceiverDiscoveryService();
        _receiverSessionManager = new ReceiverSessionManager(discovery, options.ClientName, options.FlatBufferHelloPort);
        _audioStreamingCoordinator = new AudioStreamingCoordinator(options.AudioPcmPort, OnAudioFrameSent);
        _inputProviderFactory = new DefaultAudioInputProviderFactory();
        _bridgeHost = new BridgeHost(options, _receiverSessionManager, _audioStreamingCoordinator, _inputProviderFactory);
        _bridgeHost.StateChanged += OnStateChanged;
        _commands = new BridgeCommandService(_bridgeHost);

        _logger.Info(
            $"Bridge host created. InputKind={options.InputKind}, StartupBurstFrames={options.StartupBurstFrames}, " +
            $"ManualHost={options.ManualHost ?? "AUTO"}, LastReceiverHost={options.LastReceiverHost ?? "NONE"}");
    }

    private void OnAudioFrameSent(AudioSendTelemetrySample sample)
    {
        _metrics.AddTelemetry(sample);
    }

    private void OnStateChanged(BridgeRuntimeState state)
    {
        _logger.Info(
            $"State changed: {state.State}, ReceiverHost={state.ReceiverHost ?? "NONE"}, " +
            $"ReceiverPort={(state.ReceiverPort?.ToString() ?? "NONE")}, LastError={state.LastError ?? "NONE"}");

        string? details = state.State switch
        {
            BridgeStreamState.ReceiverSelected => $"{state.ReceiverHost}:{state.ReceiverPort}",
            BridgeStreamState.Faulted => state.LastError,
            BridgeStreamState.WaitingForReceiver => state.LastError,
            _ => null
        };

        _metrics.RecordStateTransition(state.State.ToString(), details);

        if (state.State == BridgeStreamState.Streaming)
        {
            _metrics.MarkStreamingStarted();
        }
        else if (state.State == BridgeStreamState.StreamConfigured ||
                 state.State == BridgeStreamState.Idle ||
                 state.State == BridgeStreamState.WaitingForReceiver)
        {
            _metrics.MarkStreamingStopped();
        }

        if (state.State == BridgeStreamState.Faulted)
        {
            _metrics.IncrementFault(state.LastError);
        }

        if (state.State == BridgeStreamState.ReceiverSelected &&
            !string.IsNullOrWhiteSpace(state.ReceiverHost) &&
            state.ReceiverPort is int port)
        {
            _settings.LastReceiverHost = state.ReceiverHost;
            _settings.LastReceiverPort = port;
            _settings.LastReceiverName = state.ReceiverName;
            _settings.LastReceiverSeenUtc = DateTimeOffset.UtcNow;
            _settingsService.Save(_settings);
        }

        StateChanged?.Invoke(state);
    }

    private static bool IsReceiverUnavailable(InvalidOperationException ex) =>
        ex.Message.Contains("No receiver discovered", StringComparison.OrdinalIgnoreCase);

    public async ValueTask DisposeAsync()
    {
        if (_bridgeHost is not null)
        {
            _bridgeHost.StateChanged -= OnStateChanged;
            await _bridgeHost.DisposeAsync();
        }

        _commands = null;
        _bridgeHost = null;
        _inputProviderFactory = null;
        _audioStreamingCoordinator = null;
        _receiverSessionManager = null;
        _lifecycleGate.Dispose();
    }
}