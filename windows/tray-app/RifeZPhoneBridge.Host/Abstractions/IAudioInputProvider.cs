using RifeZPhoneBridge.Core.Audio;

namespace RifeZPhoneBridge.Host.Abstractions;

public interface IAudioInputProvider
{
    IPcmFrameSource CreateSource(int sampleRate, int channels);
}