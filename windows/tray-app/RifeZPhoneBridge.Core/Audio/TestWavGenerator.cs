using NAudio.Wave;

namespace RifeZPhoneBridge.Core.Audio;

public static class TestWavGenerator
{
    public static string EnsureTestToneWav(
        string outputPath,
        int sampleRate = 48000,
        int channels = 2,
        double frequencyHz = 440.0,
        short amplitude = 6000,
        double durationSeconds = 8.0)
    {
        if (File.Exists(outputPath))
            return outputPath;

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var generator = new PcmToneGenerator(
            sampleRate: sampleRate,
            channels: channels,
            frequencyHz: frequencyHz,
            amplitude: amplitude);

        int totalSamples = (int)(sampleRate * durationSeconds);
        int remainingSamples = totalSamples;

        using var writer = new WaveFileWriter(
            outputPath,
            new WaveFormat(sampleRate, 16, channels));

        const int chunkSamples = 1920;

        while (remainingSamples > 0)
        {
            int currentChunk = Math.Min(chunkSamples, remainingSamples);
            byte[] payload = generator.GenerateFrame(currentChunk);
            writer.Write(payload, 0, payload.Length);
            remainingSamples -= currentChunk;
        }

        return outputPath;
    }
}