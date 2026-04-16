using NAudio.Wave;
using RifeZPhoneBridge.Core.Audio;

namespace RifeZPhoneBridge.DriverCompanion;

public sealed class DriverWavSource : IPcmFrameSource
{
    private readonly WaveFileReader _reader;
    private readonly ISampleProvider _sampleProvider;

    public int SampleRate { get; }
    public int Channels { get; }

    public DriverWavSource(string wavPath)
    {
        _reader = new WaveFileReader(wavPath);

        if (_reader.WaveFormat.Encoding != WaveFormatEncoding.Pcm &&
            _reader.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
        {
            throw new InvalidOperationException("Unsupported WAV encoding.");
        }

        SampleRate = _reader.WaveFormat.SampleRate;
        Channels = _reader.WaveFormat.Channels;

        _sampleProvider = _reader.ToSampleProvider();
    }

    public byte[]? ReadFrame(int frameSamples)
    {
        int sampleCount = frameSamples * Channels;
        float[] floatBuffer = new float[sampleCount];
        int read = _sampleProvider.Read(floatBuffer, 0, sampleCount);

        if (read <= 0)
            return null;

        byte[] pcm = new byte[read * sizeof(short)];

        for (int i = 0; i < read; i++)
        {
            float sample = Math.Clamp(floatBuffer[i], -1.0f, 1.0f);
            short pcm16 = (short)Math.Round(sample * short.MaxValue);
            BitConverter.GetBytes(pcm16).CopyTo(pcm, i * sizeof(short));
        }

        return pcm;
    }

    public void Dispose()
    {
        _reader.Dispose();
    }
}