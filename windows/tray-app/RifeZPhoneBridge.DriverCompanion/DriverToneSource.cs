using RifeZPhoneBridge.Core.Audio;

namespace RifeZPhoneBridge.DriverCompanion;

public sealed class DriverToneSource : IPcmFrameSource
{
    private readonly PcmToneGenerator _generator;
    private readonly int? _maxFrames;
    private int _framesSent;

    public int SampleRate { get; }
    public int Channels { get; }

    public DriverToneSource(int sampleRate, int channels, int? maxFrames = null)
    {
        SampleRate = sampleRate;
        Channels = channels;
        _maxFrames = maxFrames;

        _generator = new PcmToneGenerator(
            sampleRate: sampleRate,
            channels: channels,
            frequencyHz: 440.0,
            amplitude: 6000);
    }

    public byte[]? ReadFrame(int frameSamples)
    {
        if (_maxFrames.HasValue && _framesSent >= _maxFrames.Value)
            return null;

        _framesSent++;
        return _generator.GenerateFrame(frameSamples);
    }

    public void Dispose()
    {
    }
}