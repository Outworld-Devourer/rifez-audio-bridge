using RifeZPhoneBridge.Core.Audio;

namespace RifeZPhoneBridge.DriverCompanion;

public sealed class ToneProducer : IAudioProducer
{
    private readonly PcmToneGenerator _generator;
    private readonly int? _maxFrames;
    private int _framesSent;
    private bool _started;

    public int SampleRate { get; }
    public int Channels { get; }

    public ToneProducer(int sampleRate, int channels, int? maxFrames = null)
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

    public void Start()
    {
        _started = true;
    }

    public byte[]? ReadFrame(int frameSamples)
    {
        if (!_started)
            throw new InvalidOperationException("Producer is not started.");

        if (_maxFrames.HasValue && _framesSent >= _maxFrames.Value)
            return null;

        _framesSent++;
        return _generator.GenerateFrame(frameSamples);
    }

    public void Stop()
    {
        _started = false;
    }

    public void Dispose()
    {
        Stop();
    }
}