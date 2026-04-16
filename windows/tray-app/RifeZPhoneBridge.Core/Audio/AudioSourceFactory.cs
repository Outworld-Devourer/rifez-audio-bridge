namespace RifeZPhoneBridge.Core.Audio;

public static class AudioSourceFactory
{
    public static IPcmFrameSource Create(
        AudioSourceMode mode,
        string? wavPath,
        int sampleRate,
        int channels)
    {
        return mode switch
        {
            AudioSourceMode.Tone => new TonePcmFrameSource(
                sampleRate: sampleRate,
                channels: channels),

            AudioSourceMode.Wav => new WavPcmFrameSource(
                wavPath ?? throw new ArgumentNullException(nameof(wavPath), "WAV mode requires a file path.")),

            AudioSourceMode.LiveLoopback => new WasapiLoopbackPcmSource(
                sampleRate: sampleRate,
                channels: channels),

            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}