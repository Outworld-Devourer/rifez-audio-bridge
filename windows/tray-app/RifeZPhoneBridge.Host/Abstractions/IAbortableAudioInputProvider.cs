namespace RifeZPhoneBridge.Host.Abstractions;

public interface IAbortableAudioInputProvider
{
    void AbortActiveSource();
}