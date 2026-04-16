using RifeZPhoneBridge.DriverCompanion.Abstractions;

namespace RifeZPhoneBridge.DriverCompanion.Normalization;

public sealed class FloatToPcm16Normalizer : IAudioFrameNormalizer
{
    public int OutputSampleRate { get; }
    public int OutputChannels { get; }

    public FloatToPcm16Normalizer(int outputSampleRate, int outputChannels)
    {
        OutputSampleRate = outputSampleRate;
        OutputChannels = outputChannels;
    }

    public int ConvertToPcm16(
        float[] input,
        int inputCount,
        short[] output,
        int outputOffset,
        int outputCapacity)
    {
        int samplesToConvert = Math.Min(inputCount, outputCapacity);

        for (int i = 0; i < samplesToConvert; i++)
        {
            float clamped = Math.Clamp(input[i], -1.0f, 1.0f);
            output[outputOffset + i] = (short)Math.Round(clamped * short.MaxValue);
        }

        return samplesToConvert;
    }
}