using System.Diagnostics;

namespace RifeZPhoneBridge.Core.Audio;

public sealed class PcmStreamingSession
{
    public async Task StreamAsync(
        Stream networkStream,
        IPcmFrameSource source,
        int frameSamples,
        int startupBurstFrames = 24,
        CancellationToken cancellationToken = default)
    {
        long presentationIndex = 0;
        double frameDurationMs = frameSamples * 1000.0 / source.SampleRate;

        var stopwatch = Stopwatch.StartNew();
        int frameIndex = 0;

        while (true)
        {
            byte[]? payload = source.ReadFrame(frameSamples);
            if (payload is null)
                break;

            await AudioFrameWriter.WritePcm16FrameAsync(
                networkStream,
                payload,
                presentationIndex,
                cancellationToken);

            int samplesInPayload = payload.Length / (source.Channels * sizeof(short));
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
}