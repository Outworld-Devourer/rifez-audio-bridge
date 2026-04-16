using RifeZPhoneBridge.Core.Audio;
using RifeZPhoneBridge.Host.Abstractions;

namespace RifeZPhoneBridge.Host.Sources;

public sealed class WavAudioInputProviderPlaceholder : IAudioInputProvider
{
    private readonly string? _sourcePath;

    public WavAudioInputProviderPlaceholder(string? sourcePath)
    {
        _sourcePath = sourcePath;
    }

    public IPcmFrameSource CreateSource(int sampleRate, int channels)
    {
        throw new NotSupportedException(
            $"WAV audio input is not implemented yet. Requested path: {_sourcePath ?? "NONE"}");
    }
}