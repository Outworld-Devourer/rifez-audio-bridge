using RifeZPhoneBridge.Core.Audio;
using RifeZPhoneBridge.Host.Abstractions;

namespace RifeZPhoneBridge.Host.Sources;

public sealed class DriverAudioInputProvider : IAudioInputProvider
{
    public IPcmFrameSource CreateSource(int sampleRate, int channels)
    {
        return new DriverControlPcmFrameSource(sampleRate, channels);
    }
}