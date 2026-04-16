using RifeZPhoneBridge.Host.Abstractions;
using RifeZPhoneBridge.Host.Models;
using RifeZPhoneBridge.Host.Sources;

namespace RifeZPhoneBridge.Host.Services;

public sealed class DefaultAudioInputProviderFactory : IAudioInputProviderFactory
{
    public IAudioInputProvider CreateProvider(AudioInputSessionOptions options)
    {
        return options.InputKind switch
        {
            AudioInputKind.Loopback => new LoopbackAudioInputProvider(),
            AudioInputKind.Driver => new DriverAudioInputProvider(),
            AudioInputKind.Wav => new WavAudioInputProviderPlaceholder(options.SourcePath),
            _ => throw new InvalidOperationException($"Unsupported audio input kind: {options.InputKind}")
        };
    }
}