using RifeZPhoneBridge.Host.Abstractions;
using RifeZPhoneBridge.Host.Models;

namespace RifeZPhoneBridge.Host.Services;

public sealed class BridgeCommandService : IBridgeCommandService
{
    private readonly IBridgeHost _bridgeHost;

    public BridgeCommandService(IBridgeHost bridgeHost)
    {
        _bridgeHost = bridgeHost;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_bridgeHost.CurrentState.State is BridgeStreamState.Idle
            or BridgeStreamState.WaitingForReceiver
            or BridgeStreamState.Faulted)
        {
            await _bridgeHost.InitializeAsync(cancellationToken);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_bridgeHost.CurrentState.State == BridgeStreamState.Streaming)
            return;

        if (_bridgeHost.CurrentState.State is BridgeStreamState.Idle
            or BridgeStreamState.WaitingForReceiver
            or BridgeStreamState.Faulted)
        {
            await _bridgeHost.InitializeAsync(cancellationToken);
        }

        if (_bridgeHost.CurrentState.State != BridgeStreamState.StreamConfigured)
            return;

        await _bridgeHost.StartStreamingAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_bridgeHost.CurrentState.State != BridgeStreamState.Streaming)
            return;

        await _bridgeHost.StopStreamingAsync(cancellationToken);
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        if (_bridgeHost.CurrentState.State == BridgeStreamState.Idle)
            return;

        await _bridgeHost.ShutdownAsync(cancellationToken);
    }

    public BridgeStatusSnapshot GetStatus()
    {
        var state = _bridgeHost.CurrentState;

        return new BridgeStatusSnapshot(
            State: state.State,
            ReceiverHost: state.ReceiverHost,
            ReceiverPort: state.ReceiverPort,
            LastError: state.LastError,
            IsInitialized: state.State is not BridgeStreamState.Idle and not BridgeStreamState.WaitingForReceiver,
            IsStreaming: state.State == BridgeStreamState.Streaming
        );
    }
}