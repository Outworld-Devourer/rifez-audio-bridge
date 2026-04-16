using System.IO.Pipes;
using System.Text;
using RifeZPhoneBridge.Core.Audio;
using RifeZPhoneBridge.Host.Models;

internal static class DriverPipePcmWriterClient
{
    public static async Task SendToneAsync(
        int sampleRate = 48000,
        int channels = 2,
        int frameSamples = 480,
        int frameCount = 400,
        CancellationToken cancellationToken = default)
    {
        using var pipe = new NamedPipeClientStream(
            ".",
            DriverIngressDefaults.DefaultPipeName,
            PipeDirection.Out,
            PipeOptions.Asynchronous);

        await pipe.ConnectAsync(5000, cancellationToken);

        var generator = new PcmToneGenerator(
            sampleRate: sampleRate,
            channels: channels,
            frequencyHz: 440.0,
            amplitude: 6000);

        long presentationIndex = 0;
        double frameDurationMs = frameSamples * 1000.0 / sampleRate;

        for (int i = 0; i < frameCount; i++)
        {
            byte[] payload = generator.GenerateFrame(frameSamples);

            await AudioFrameWriter.WritePcm16FrameAsync(
                pipe,
                payload,
                presentationIndex,
                cancellationToken);

            presentationIndex += frameSamples;
            await Task.Delay(TimeSpan.FromMilliseconds(frameDurationMs), cancellationToken);
        }

        await pipe.FlushAsync(cancellationToken);
    }
}