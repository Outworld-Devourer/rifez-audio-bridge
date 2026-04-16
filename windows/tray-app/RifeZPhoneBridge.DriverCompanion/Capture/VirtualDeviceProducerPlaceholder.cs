using RifeZPhoneBridge.DriverCompanion.Capture;
using RifeZPhoneBridge.DriverCompanion.Normalization;

namespace RifeZPhoneBridge.DriverCompanion;

public static class VirtualDeviceProducerPlaceholder
{
    public static IAudioProducer Create(int sampleRate, int channels, int? durationMs = null)
    {
        long? maxSamplesTotal = null;

        if (durationMs.HasValue && durationMs.Value > 0)
        {
            maxSamplesTotal = (long)Math.Ceiling(
                sampleRate * channels * (durationMs.Value / 1000.0));
        }

        var adapter = new ToneCaptureAdapter(
            sampleRate,
            channels,
            maxSamplesTotal: maxSamplesTotal);

        var normalizer = new FloatToPcm16Normalizer(sampleRate, channels);

        return new VirtualDeviceProducer(
            adapter,
            normalizer,
            outputSampleRate: sampleRate,
            outputChannels: channels);
    }
}