using RifeZPhoneBridge.Host.Models;

namespace RifeZPhoneBridge.Host.Abstractions;

public interface IBridgeCommandService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
    BridgeStatusSnapshot GetStatus();
}