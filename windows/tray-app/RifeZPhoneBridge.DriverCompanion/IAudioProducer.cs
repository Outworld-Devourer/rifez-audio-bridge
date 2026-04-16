using RifeZPhoneBridge.Core.Audio;

namespace RifeZPhoneBridge.DriverCompanion;

public interface IAudioProducer : IDisposable
{
    int SampleRate { get; }
    int Channels { get; }

    void Start();
    byte[]? ReadFrame(int frameSamples);
    void Stop();
}