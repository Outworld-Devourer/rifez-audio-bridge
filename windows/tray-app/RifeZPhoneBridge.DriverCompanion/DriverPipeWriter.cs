using System.Diagnostics;
using System.IO.Pipes;
using RifeZPhoneBridge.Core.Audio;

namespace RifeZPhoneBridge.DriverCompanion;

public sealed class DriverPipeWriter : IAsyncDisposable
{
    private readonly string _pipeName;
    private NamedPipeClientStream? _pipe;

    public DriverPipeWriter(string pipeName)
    {
        _pipeName = pipeName;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _pipe = new NamedPipeClientStream(
            ".",
            _pipeName,
            PipeDirection.Out,
            PipeOptions.Asynchronous);

        await _pipe.ConnectAsync(5000, cancellationToken);
    }

    public async Task SendProducerAsync(
        IAudioProducer producer,
        int frameSamples,
        CancellationToken cancellationToken = default)
    {
        if (_pipe is null)
            throw new InvalidOperationException("Driver pipe is not connected.");

        producer.Start();

        try
        {
            long presentationIndex = 0;
            double frameDurationMs = frameSamples * 1000.0 / producer.SampleRate;
            var stopwatch = Stopwatch.StartNew();
            int frameIndex = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                byte[]? payload = producer.ReadFrame(frameSamples);
                if (payload is null)
                    break;

                await AudioFrameWriter.WritePcm16FrameAsync(
                    _pipe,
                    payload,
                    presentationIndex,
                    cancellationToken);

                int samplesInPayload = payload.Length / (producer.Channels * sizeof(short));
                presentationIndex += samplesInPayload;

                double targetMs = (frameIndex + 1) * frameDurationMs;
                while (stopwatch.Elapsed.TotalMilliseconds < targetMs)
                {
                    await Task.Delay(1, cancellationToken);
                }

                frameIndex++;
            }

            await _pipe.FlushAsync(cancellationToken);
        }
        finally
        {
            producer.Stop();
        }
    }

    public ValueTask DisposeAsync()
    {
        _pipe?.Dispose();
        _pipe = null;
        return ValueTask.CompletedTask;
    }
}