using RifeZPhoneBridge.Host.Models;

namespace RifeZPhoneBridge.Host.Abstractions;

public interface IAudioInputProviderFactory
{
    IAudioInputProvider CreateProvider(AudioInputSessionOptions options);
}