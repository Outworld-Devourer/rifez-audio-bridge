using System.IO;
using RifeZPhoneBridge.Core.Audio;

namespace RifeZPhoneBridge.Host.Services;

public sealed class AudioStreamingCoordinator
{
    private readonly int _audioPcmPort;
    private readonly Action<AudioSendTelemetrySample>? _onFrameSent;
    private AudioPcmClient? _activeClient;
    private readonly object _sync = new();

    public AudioStreamingCoordinator(
        int audioPcmPort,
        Action<AudioSendTelemetrySample>? onFrameSent = null)
    {
        _audioPcmPort = audioPcmPort;
        _onFrameSent = onFrameSent;
    }

    public async Task RunAsync(
        string host,
        IPcmFrameSource source,
        int frameSamples,
        int startupBurstFrames,
        CancellationToken cancellationToken = default)
    {
        await using var audioClient = new AudioPcmClient();

        lock (_sync)
        {
            _activeClient = audioClient;
        }

        try
        {
            await audioClient.ConnectAsync(host, _audioPcmPort, cancellationToken);

            try
            {
                await audioClient.SendSourceAsync(
                    source,
                    frameSamples: frameSamples,
                    startupBurstFrames: startupBurstFrames,
                    onFrameSent: _onFrameSent,
                    cancellationToken: cancellationToken);
            }
            catch (IOException)
            {
                // Receiver closed the audio session intentionally.
            }
            catch (OperationCanceledException)
            {
                // Normal stop path.
            }
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_activeClient, audioClient))
                {
                    _activeClient = null;
                }
            }
        }
    }

    public async Task AbortCurrentStreamAsync()
    {
        AudioPcmClient? client;

        lock (_sync)
        {
            client = _activeClient;
            _activeClient = null;
        }

        if (client is not null)
        {
            try
            {
                await client.DisposeAsync();
            }
            catch
            {
            }
        }
    }
}