namespace RifeZPhoneBridge.Core.Audio;

public sealed class TonePcmFrameSource : IPcmFrameSource
{
    private readonly PcmToneGenerator _generator;

    public int SampleRate { get; }
    public int Channels { get; }

    public TonePcmFrameSource(
        int sampleRate = 48000,
        int channels = 2,
        double frequencyHz = 440.0,
        short amplitude = 6000)
    {
        SampleRate = sampleRate;
        Channels = channels;
        _generator = new PcmToneGenerator(sampleRate, channels, frequencyHz, amplitude);
    }

    public byte[]? ReadFrame(int frameSamples) => _generator.GenerateFrame(frameSamples);

    public void Dispose()
    {
    }
}