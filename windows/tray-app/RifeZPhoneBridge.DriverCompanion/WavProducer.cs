using NAudio.Wave;

namespace RifeZPhoneBridge.DriverCompanion;

public sealed class WavProducer : IAudioProducer
{
    private readonly WaveFileReader _reader;
    private readonly ISampleProvider _sampleProvider;
    private bool _started;

    public int SampleRate { get; }
    public int Channels { get; }

    public WavProducer(string wavPath)
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

    public void Start()
    {
        _started = true;
    }

    public byte[]? ReadFrame(int frameSamples)
    {
        if (!_started)
            throw new InvalidOperationException("Producer is not started.");

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

    public void Stop()
    {
        _started = false;
    }

    public void Dispose()
    {
        Stop();
        _reader.Dispose();
    }
}