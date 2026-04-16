using NAudio.Wave;

namespace RifeZPhoneBridge.Core.Audio;

public sealed class WavPcmFrameSource : IPcmFrameSource
{
    private readonly WaveFileReader _reader;
    private readonly int _bytesPerFrameSample;

    public int SampleRate => _reader.WaveFormat.SampleRate;
    public int Channels => _reader.WaveFormat.Channels;

    public WavPcmFrameSource(string wavPath)
    {
        _reader = new WaveFileReader(wavPath);

        if (_reader.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
            throw new InvalidOperationException("Only PCM WAV is supported in this first WAV source.");

        if (_reader.WaveFormat.BitsPerSample != 16)
            throw new InvalidOperationException("Only 16-bit PCM WAV is supported in this first WAV source.");

        if (_reader.WaveFormat.Channels != 2)
            throw new InvalidOperationException("Only stereo WAV is supported in this first WAV source.");

        if (_reader.WaveFormat.SampleRate != 48000)
            throw new InvalidOperationException("Only 48 kHz WAV is supported in this first WAV source.");

        _bytesPerFrameSample = _reader.WaveFormat.BlockAlign;
    }

    public byte[]? ReadFrame(int frameSamples)
    {
        int bytesToRead = frameSamples * _bytesPerFrameSample;
        byte[] buffer = new byte[bytesToRead];

        int read = _reader.Read(buffer, 0, bytesToRead);
        if (read <= 0)
            return null;

        if (read == bytesToRead)
            return buffer;

        byte[] trimmed = new byte[read];
        Buffer.BlockCopy(buffer, 0, trimmed, 0, read);
        return trimmed;
    }

    public void Dispose()
    {
        _reader.Dispose();
    }
}