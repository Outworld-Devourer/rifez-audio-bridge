namespace RifeZPhoneBridge.DriverCompanion.Abstractions;

public interface IAudioFrameNormalizer
{
    int OutputSampleRate { get; }
    int OutputChannels { get; }

    int ConvertToPcm16(
        float[] input,
        int inputCount,
        short[] output,
        int outputOffset,
        int outputCapacity);
}