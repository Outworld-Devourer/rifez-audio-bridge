namespace RifeZPhoneBridge.DriverCompanion.Abstractions;

public interface IAudioCaptureAdapter : IDisposable
{
    int InputSampleRate { get; }
    int InputChannels { get; }

    void Start();
    int ReadSamples(float[] buffer, int offset, int count);
    void Stop();
}