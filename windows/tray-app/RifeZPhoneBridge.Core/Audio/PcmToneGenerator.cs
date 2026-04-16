using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RifeZPhoneBridge.Core.Audio;

public sealed class PcmToneGenerator
{
    private readonly int _sampleRate;
    private readonly int _channels;
    private readonly double _frequencyHz;
    private readonly short _amplitude;
    private double _phase;

    public PcmToneGenerator(
        int sampleRate = 48000,
        int channels = 2,
        double frequencyHz = 440.0,
        short amplitude = 6000)
    {
        _sampleRate = sampleRate;
        _channels = channels;
        _frequencyHz = frequencyHz;
        _amplitude = amplitude;
        _phase = 0.0;
    }

    public byte[] GenerateFrame(int frameSamples)
    {
        short[] samples = new short[frameSamples * _channels];
        double phaseIncrement = 2.0 * Math.PI * _frequencyHz / _sampleRate;

        for (int i = 0; i < frameSamples; i++)
        {
            short sample = (short)(_amplitude * Math.Sin(_phase));
            _phase += phaseIncrement;
            if (_phase > 2.0 * Math.PI)
                _phase -= 2.0 * Math.PI;

            for (int ch = 0; ch < _channels; ch++)
            {
                samples[i * _channels + ch] = sample;
            }
        }

        byte[] payload = new byte[samples.Length * sizeof(short)];
        Buffer.BlockCopy(samples, 0, payload, 0, payload.Length);
        return payload;
    }
}