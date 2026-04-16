using RifeZPhoneBridge.Host.Models;

namespace RifeZPhoneBridge.Host.Abstractions;

public interface IBridgeHost : IAsyncDisposable
{
    BridgeRuntimeState CurrentState { get; }

    event Action<BridgeRuntimeState>? StateChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task StartStreamingAsync(CancellationToken cancellationToken = default);
    Task StopStreamingAsync(CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}