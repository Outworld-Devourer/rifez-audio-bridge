using System.IO;
using System.Net.Sockets;
using RifeZ.PhoneAudio.Control;
using RifeZPhoneBridge.Core.Discovery;
using RifeZPhoneBridge.Core.Models;
using RifeZPhoneBridge.Core.Networking;

namespace RifeZPhoneBridge.Host.Services;

public sealed class ReceiverSessionManager : IAsyncDisposable
{
    private readonly IReceiverDiscoveryService _discoveryService;
    private readonly string _clientName;
    private readonly int _flatBufferHelloPort;
    private FlatBufferHelloClient? _client;

    public ReceiverSessionManager(
        IReceiverDiscoveryService discoveryService,
        string clientName,
        int flatBufferHelloPort)
    {
        _discoveryService = discoveryService;
        _clientName = clientName;
        _flatBufferHelloPort = flatBufferHelloPort;
    }

    public async Task<PhoneEndpoint> ResolveEndpointAsync(
        string? preferredHost,
        int preferredPort,
        string? manualHost,
        int manualPort,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(preferredHost))
        {
            return new PhoneEndpoint(preferredHost, preferredPort, "Last Known Receiver");
        }

        return await DiscoverEndpointAsync(manualHost, manualPort, cancellationToken);
    }

    public async Task<PhoneEndpoint> DiscoverEndpointAsync(
        string? manualHost,
        int manualPort,
        CancellationToken cancellationToken = default)
    {
        var discovered = await _discoveryService.DiscoverAsync();

        if (discovered.Count > 0)
        {
            return PhoneEndpointMapper.FromNsdReceiver(discovered[0]);
        }

        if (!string.IsNullOrWhiteSpace(manualHost))
        {
            var fallback = new NsdReceiverInfo(
                serviceName: "Manual Receiver",
                host: manualHost,
                port: manualPort);

            return PhoneEndpointMapper.FromNsdReceiver(fallback);
        }

        throw new InvalidOperationException(
            $"No receiver discovered. Manual fallback host is '{manualHost ?? "null"}', port={manualPort}.");
    }

    public async Task ConnectAsync(PhoneEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        bool isCachedReceiver =
            string.Equals(endpoint.DisplayName, "Last Known Receiver", StringComparison.Ordinal);

        if (isCachedReceiver)
        {
            await ConnectWithRetryAsync(endpoint, cancellationToken);
            return;
        }

        await ConnectOnceAsync(endpoint, cancellationToken);
    }

    private async Task ConnectWithRetryAsync(PhoneEndpoint endpoint, CancellationToken cancellationToken)
    {
        // Cached receiver retry ladder:
        // immediate try + short backoff attempts for the common case where
        // Android has restarted and discovery/cached host is valid, but the
        // control socket is not yet listening.
        int[] retryDelaysMs = new[] { 0, 200, 500, 1000, 1500, 2500 };

        Exception? lastError = null;

        for (int attempt = 0; attempt < retryDelaysMs.Length; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int delayMs = retryDelaysMs[attempt];
            if (delayMs > 0)
            {
                await Task.Delay(delayMs, cancellationToken);
            }

            try
            {
                await ConnectOnceAsync(endpoint, cancellationToken);
                return;
            }
            catch (Exception ex) when (IsRetryableControlConnectFailure(ex))
            {
                lastError = ex;
            }
        }

        throw new InvalidOperationException(
            $"Failed to establish control session with cached receiver {endpoint.Host}:{_flatBufferHelloPort} after retry attempts. " +
            $"{lastError?.Message}",
            lastError);
    }

    private async Task ConnectOnceAsync(PhoneEndpoint endpoint, CancellationToken cancellationToken)
    {
        if (_client is not null)
        {
            try
            {
                await _client.DisposeAsync();
            }
            catch
            {
            }

            _client = null;
        }

        _client = new FlatBufferHelloClient();

        try
        {
            await _client.ConnectAsync(endpoint.Host, _flatBufferHelloPort, cancellationToken);
            await _client.HelloAsync(_clientName);
        }
        catch
        {
            try
            {
                await _client.DisposeAsync();
            }
            catch
            {
            }

            _client = null;
            throw;
        }
    }

    private static bool IsRetryableControlConnectFailure(Exception ex)
    {
        if (ex is SocketException socketEx)
        {
            return socketEx.SocketErrorCode == SocketError.ConnectionRefused ||
                   socketEx.SocketErrorCode == SocketError.ConnectionReset;
        }

        if (ex is IOException ioEx && ioEx.InnerException is SocketException innerSocketEx)
        {
            return innerSocketEx.SocketErrorCode == SocketError.ConnectionRefused ||
                   innerSocketEx.SocketErrorCode == SocketError.ConnectionReset;
        }

        return false;
    }

    public async Task<dynamic> ConfigureAndStartAsync(
        int sampleRate,
        int channels,
        SampleFormat sampleFormat,
        CodecType codec,
        int frameSamples)
    {
        if (_client is null)
            throw new InvalidOperationException("Control client not connected.");

        var config = await _client.ConfigureStreamAsync(
            sampleRate: (uint)sampleRate,
            channels: (byte)channels,
            sampleFormat: sampleFormat,
            codec: codec,
            frameSamples: (uint)frameSamples);

        if (!config.Accepted)
            throw new InvalidOperationException($"Receiver rejected stream config: {config.Reason}");

        bool started = await _client.StartStreamAsync();
        if (!started)
            throw new InvalidOperationException("Receiver did not acknowledge START_STREAM.");

        return config;
    }

    public async Task DisconnectAsync()
    {
        if (_client is null)
            return;

        try
        {
            await _client.DisconnectAsync();
        }
        catch
        {
        }

        try
        {
            await _client.DisposeAsync();
        }
        catch
        {
        }

        _client = null;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}