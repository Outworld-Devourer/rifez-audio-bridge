namespace RifeZPhoneBridge.Core.Audio;

public interface IPcmFrameSource : IDisposable
{
    int SampleRate { get; }
    int Channels { get; }

    /// <summary>
    /// Fills and returns one PCM16 interleaved frame payload.
    /// Returns null when the source is finished.
    /// </summary>
    byte[]? ReadFrame(int frameSamples);
}