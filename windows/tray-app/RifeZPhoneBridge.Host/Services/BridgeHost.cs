using RifeZ.PhoneAudio.Control;
using RifeZPhoneBridge.Host.Abstractions;
using RifeZPhoneBridge.Host.Models;

namespace RifeZPhoneBridge.Host.Services;

public sealed class BridgeHost : IBridgeHost
{
    private readonly BridgeHostOptions _options;
    private readonly ReceiverSessionManager _receiverSessionManager;
    private readonly AudioStreamingCoordinator _audioStreamingCoordinator;
    private readonly IAudioInputProviderFactory _audioInputProviderFactory;
    private IAudioInputProvider? _activeAudioInputProvider;

    private CancellationTokenSource? _streamCts;
    private Task? _streamTask;
    private dynamic? _activeConfig;

    public BridgeRuntimeState CurrentState { get; private set; } =
        new(BridgeStreamState.Idle, null, null, null, null, null);

    public event Action<BridgeRuntimeState>? StateChanged;

    public BridgeHost(
        BridgeHostOptions options,
        ReceiverSessionManager receiverSessionManager,
        AudioStreamingCoordinator audioStreamingCoordinator,
        IAudioInputProviderFactory audioInputProviderFactory)
    {
        _options = options;
        _receiverSessionManager = receiverSessionManager;
        _audioStreamingCoordinator = audioStreamingCoordinator;
        _audioInputProviderFactory = audioInputProviderFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentState.State is not BridgeStreamState.Idle
            and not BridgeStreamState.WaitingForReceiver
            and not BridgeStreamState.Faulted)
        {
            return;
        }

        try
        {
            SetState(CurrentState with
            {
                State = BridgeStreamState.Discovering,
                LastError = null
            });

            var endpoint = await _receiverSessionManager.ResolveEndpointAsync(
                preferredHost: _options.LastReceiverHost,
                preferredPort: _options.LastReceiverPort,
                manualHost: _options.ManualHost,
                manualPort: _options.ManualPort,
                cancellationToken: cancellationToken);

            SetState(CurrentState with
            {
                State = BridgeStreamState.ReceiverSelected,
                ReceiverName = endpoint.DisplayName,
                ReceiverHost = endpoint.Host,
                ReceiverPort = endpoint.Port,
                SelectedEndpoint = endpoint,
                LastError = null
            });

            await _receiverSessionManager.ConnectAsync(endpoint, cancellationToken);

            SetState(CurrentState with
            {
                State = BridgeStreamState.ControlConnected,
                LastError = null
            });

            _activeConfig = await _receiverSessionManager.ConfigureAndStartAsync(
                sampleRate: _options.SampleRate,
                channels: _options.Channels,
                sampleFormat: SampleFormat.PCM16,
                codec: CodecType.PCM,
                frameSamples: _options.FrameSamples);

            SetState(CurrentState with
            {
                State = BridgeStreamState.StreamConfigured,
                LastError = null
            });
        }
        catch (Exception ex)
        {
            try
            {
                await _receiverSessionManager.DisconnectAsync();
            }
            catch
            {
            }

            _activeConfig = null;

            SetState(CurrentState with
            {
                State = BridgeStreamState.WaitingForReceiver,
                ReceiverName = null,
                ReceiverHost = null,
                ReceiverPort = null,
                SelectedEndpoint = null,
                LastError = ex.Message
            });

            throw;
        }
    }

    public Task StartStreamingAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentState.SelectedEndpoint is null)
            throw new InvalidOperationException("Host is not initialized.");

        if (_activeConfig is null)
            throw new InvalidOperationException("Stream is not configured.");

        if (_streamTask is not null && !_streamTask.IsCompleted)
            return Task.CompletedTask;

        _streamCts?.Dispose();
        _streamCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var endpoint = CurrentState.SelectedEndpoint;
        dynamic config = _activeConfig;
        var streamToken = _streamCts.Token;

        _streamTask = Task.Run(async () =>
        {
            bool completedNormally = false;

            try
            {
                var inputOptions = new AudioInputSessionOptions(
                    InputKind: _options.InputKind,
                    SampleRate: (int)config.SampleRate,
                    Channels: (int)config.Channels,
                    SourcePath: _options.InputSourcePath);

                var provider = _audioInputProviderFactory.CreateProvider(inputOptions);
                _activeAudioInputProvider = provider;

                using var source = provider.CreateSource(
                    inputOptions.SampleRate,
                    inputOptions.Channels);

                SetState(CurrentState with
                {
                    State = BridgeStreamState.Streaming,
                    LastError = null
                });

                await _audioStreamingCoordinator.RunAsync(
                    host: endpoint.Host,
                    source: source,
                    frameSamples: (int)config.FrameSamples,
                    startupBurstFrames: _options.StartupBurstFrames,
                    cancellationToken: streamToken);

                completedNormally = true;
            }
            catch (OperationCanceledException)
            {
                // Normal stop path. StopStreamingAsync owns final state transition.
            }
            catch (Exception ex)
            {
                SetState(CurrentState with
                {
                    State = BridgeStreamState.WaitingForReceiver,
                    LastError = ex.Message
                });
            }
            finally
            {
                _activeAudioInputProvider = null;

                // If the streaming loop ended on its own without an explicit stop/cancel,
                // treat it as receiver/session loss instead of leaving stale Streaming state.
                if (!streamToken.IsCancellationRequested && completedNormally)
                {
                    SetState(CurrentState with
                    {
                        State = BridgeStreamState.WaitingForReceiver,
                        LastError = "Receiver session ended."
                    });
                }
            }
        }, streamToken);

        return Task.CompletedTask;
    }

    public async Task StopStreamingAsync(CancellationToken cancellationToken = default)
    {
        if (_streamTask is null)
            return;

        SetState(CurrentState with
        {
            State = BridgeStreamState.Stopping
        });

        try
        {
            _streamCts?.Cancel();
        }
        catch
        {
        }

        try
        {
            if (_activeAudioInputProvider is IAbortableAudioInputProvider abortableProvider)
            {
                abortableProvider.AbortActiveSource();
            }
        }
        catch
        {
        }

        try
        {
            await _audioStreamingCoordinator.AbortCurrentStreamAsync();
        }
        catch
        {
        }

        try
        {
            var completed = await Task.WhenAny(_streamTask, Task.Delay(3000, cancellationToken));
            if (completed != _streamTask)
            {
                SetState(CurrentState with
                {
                    State = BridgeStreamState.Faulted,
                    LastError = "Timed out while stopping active stream."
                });
                return;
            }

            await _streamTask;
        }
        catch
        {
        }

        _streamTask = null;
        _activeAudioInputProvider = null;

        _streamCts?.Dispose();
        _streamCts = null;

        SetState(CurrentState with
        {
            State = BridgeStreamState.StreamConfigured,
            LastError = null
        });
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_streamTask is not null && !_streamTask.IsCompleted)
            {
                await StopStreamingAsync(cancellationToken);
            }
        }
        catch
        {
        }

        SetState(CurrentState with
        {
            State = BridgeStreamState.Stopping
        });

        try
        {
            await _receiverSessionManager.DisconnectAsync();
        }
        catch
        {
        }

        _activeConfig = null;
        _activeAudioInputProvider = null;
        _streamTask = null;

        _streamCts?.Dispose();
        _streamCts = null;

        SetState(new BridgeRuntimeState(
            BridgeStreamState.Idle,
            null,
            null,
            null,
            null,
            null));
    }

    public async ValueTask DisposeAsync()
    {
        if (CurrentState.State != BridgeStreamState.Idle)
        {
            await ShutdownAsync();
        }

        await _receiverSessionManager.DisposeAsync();
    }

    private void SetState(BridgeRuntimeState newState)
    {
        CurrentState = newState;
        StateChanged?.Invoke(CurrentState);
    }
}