using RifeZPhoneBridge.DriverCompanion.Abstractions;

namespace RifeZPhoneBridge.DriverCompanion.Capture;

public sealed class ToneCaptureAdapter : IAudioCaptureAdapter
{
    private readonly double _frequencyHz;
    private readonly float _amplitude;
    private readonly long? _maxSamplesTotal;

    private double _phase;
    private bool _started;
    private long _samplesProduced;

    public int InputSampleRate { get; }
    public int InputChannels { get; }

    public ToneCaptureAdapter(
        int inputSampleRate,
        int inputChannels,
        double frequencyHz = 440.0,
        float amplitude = 0.2f,
        long? maxSamplesTotal = null)
    {
        InputSampleRate = inputSampleRate;
        InputChannels = inputChannels;
        _frequencyHz = frequencyHz;
        _amplitude = amplitude;
        _maxSamplesTotal = maxSamplesTotal;
    }

    public void Start()
    {
        _started = true;
        _samplesProduced = 0;
    }

    public int ReadSamples(float[] buffer, int offset, int count)
    {
        if (!_started)
            throw new InvalidOperationException("Capture adapter is not started.");

        if (_maxSamplesTotal.HasValue && _samplesProduced >= _maxSamplesTotal.Value)
            return 0;

        int samplesToGenerate = count;

        if (_maxSamplesTotal.HasValue)
        {
            long remaining = _maxSamplesTotal.Value - _samplesProduced;
            if (remaining <= 0)
                return 0;

            samplesToGenerate = (int)Math.Min(samplesToGenerate, remaining);
        }

        double phaseStep = 2.0 * Math.PI * _frequencyHz / InputSampleRate;

        for (int i = 0; i < samplesToGenerate; i += InputChannels)
        {
            float sample = (float)(Math.Sin(_phase) * _amplitude);
            _phase += phaseStep;

            if (_phase >= 2.0 * Math.PI)
                _phase -= 2.0 * Math.PI;

            for (int ch = 0; ch < InputChannels && (i + ch) < samplesToGenerate; ch++)
            {
                buffer[offset + i + ch] = sample;
            }
        }

        _samplesProduced += samplesToGenerate;
        return samplesToGenerate;
    }

    public void Stop()
    {
        _started = false;
    }

    public void Dispose()
    {
        Stop();
    }
}