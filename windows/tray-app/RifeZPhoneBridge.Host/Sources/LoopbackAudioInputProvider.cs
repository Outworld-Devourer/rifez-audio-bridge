using RifeZPhoneBridge.Core.Audio;
using RifeZPhoneBridge.Host.Abstractions;

namespace RifeZPhoneBridge.Host.Sources;

public sealed class LoopbackAudioInputProvider : IAudioInputProvider
{
    public IPcmFrameSource CreateSource(int sampleRate, int channels)
    {
        return new WasapiLoopbackPcmSource(sampleRate, channels);
    }
}