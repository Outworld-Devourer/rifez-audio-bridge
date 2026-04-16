using System.Diagnostics;
using System.Net.Sockets;

namespace RifeZPhoneBridge.Core.Audio;

public sealed class AudioPcmClient : IAsyncDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(host, port, cancellationToken);
        _stream = _client.GetStream();
    }

    public async Task SendSourceAsync(
        IPcmFrameSource source,
        int frameSamples = 1920,
        int startupBurstFrames = 6,
        Action<AudioSendTelemetrySample>? onFrameSent = null,
        CancellationToken cancellationToken = default)
    {
        NetworkStream? stream = _stream;
        if (stream is null)
            throw new InvalidOperationException("Audio PCM client is not connected.");

        long presentationIndex = 0;
        double frameDurationMs = frameSamples * 1000.0 / source.SampleRate;
        var stopwatch = Stopwatch.StartNew();
        long frameIndex = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            byte[]? payload = source.ReadFrame(frameSamples);
            if (payload is null)
                break;

            if (payload.Length == 0)
            {
                await Task.Delay(5, cancellationToken);
                continue;
            }

            int samplesInPayload = payload.Length / (source.Channels * sizeof(short));

            try
            {
                await AudioFrameWriter.WritePcm16FrameAsync(
                    stream,
                    payload,
                    presentationIndex,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (IOException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }

            onFrameSent?.Invoke(new AudioSendTelemetrySample(
                FrameIndex: frameIndex,
                PayloadBytes: payload.Length,
                SamplesInPayload: samplesInPayload,
                PresentationIndex: presentationIndex,
                ElapsedMilliseconds: stopwatch.Elapsed.TotalMilliseconds));

            presentationIndex += samplesInPayload;

            if (frameIndex >= startupBurstFrames)
            {
                double targetMs = (frameIndex - startupBurstFrames + 1) * frameDurationMs;
                while (stopwatch.Elapsed.TotalMilliseconds < targetMs)
                {
                    await Task.Delay(1, cancellationToken);
                }
            }

            frameIndex++;
        }
    }

    public async Task SendToneAsync(
        int frameSamples = 1920,
        int frameCount = 250,
        int sampleRate = 48000,
        int channels = 2,
        int startupBurstFrames = 24,
        Action<AudioSendTelemetrySample>? onFrameSent = null,
        CancellationToken cancellationToken = default)
    {
        NetworkStream? stream = _stream;
        if (stream is null)
            throw new InvalidOperationException("Audio PCM client is not connected.");

        var generator = new PcmToneGenerator(
            sampleRate: sampleRate,
            channels: channels,
            frequencyHz: 440.0,
            amplitude: 6000);

        long presentationIndex = 0;
        double frameDurationMs = frameSamples * 1000.0 / sampleRate;
        var stopwatch = Stopwatch.StartNew();

        for (long i = 0; i < frameCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            byte[] payload = generator.GenerateFrame(frameSamples);

            try
            {
                await AudioFrameWriter.WritePcm16FrameAsync(
                    stream,
                    payload,
                    presentationIndex,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (IOException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }

            onFrameSent?.Invoke(new AudioSendTelemetrySample(
                FrameIndex: i,
                PayloadBytes: payload.Length,
                SamplesInPayload: frameSamples,
                PresentationIndex: presentationIndex,
                ElapsedMilliseconds: stopwatch.Elapsed.TotalMilliseconds));

            presentationIndex += frameSamples;

            if (i >= startupBurstFrames)
            {
                double targetMs = (i - startupBurstFrames + 1) * frameDurationMs;
                while (stopwatch.Elapsed.TotalMilliseconds < targetMs)
                {
                    await Task.Delay(1, cancellationToken);
                }
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        try
        {
            _stream?.Dispose();
        }
        catch
        {
        }

        try
        {
            _client?.Dispose();
        }
        catch
        {
        }

        _stream = null;
        _client = null;
        return ValueTask.CompletedTask;
    }
}