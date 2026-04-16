namespace RifeZPhoneBridge.Host.Abstractions;

public interface IHostCommandServer : IAsyncDisposable
{
    Task RunAsync(CancellationToken cancellationToken = default);
}