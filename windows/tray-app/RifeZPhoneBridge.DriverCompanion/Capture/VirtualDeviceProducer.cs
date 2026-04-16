using RifeZPhoneBridge.DriverCompanion.Abstractions;
using RifeZPhoneBridge.DriverCompanion.Buffers;

namespace RifeZPhoneBridge.DriverCompanion.Capture;

public sealed class VirtualDeviceProducer : IAudioProducer
{
    private readonly IAudioCaptureAdapter _captureAdapter;
    private readonly IAudioFrameNormalizer _normalizer;
    private readonly PcmRingBuffer _ringBuffer;

    private readonly int _captureChunkSamples;
    private readonly int _bufferTargetSamples;
    private readonly object _stateSync = new();

    private CancellationTokenSource? _captureCts;
    private Task? _captureTask;
    private bool _started;
    private bool _stopping;
    private volatile bool _captureCompleted;

    public int SampleRate { get; }
    public int Channels { get; }

    public VirtualDeviceProducer(
        IAudioCaptureAdapter captureAdapter,
        IAudioFrameNormalizer normalizer,
        int outputSampleRate,
        int outputChannels,
        int ringBufferCapacitySamples = 48000,
        int captureChunkSamples = 1920,
        int bufferTargetSamples = 960)
    {
        _captureAdapter = captureAdapter;
        _normalizer = normalizer;
        SampleRate = outputSampleRate;
        Channels = outputChannels;
        _ringBuffer = new PcmRingBuffer(ringBufferCapacitySamples);
        _captureChunkSamples = captureChunkSamples;
        _bufferTargetSamples = bufferTargetSamples;
    }

    public void Start()
    {
        lock (_stateSync)
        {
            if (_started)
                return;

            _started = true;
            _stopping = false;
            _captureCompleted = false;
            _ringBuffer.Clear();

            _captureCts = new CancellationTokenSource();
            _captureAdapter.Start();

            _captureTask = Task.Run(() => CaptureLoop(_captureCts.Token), _captureCts.Token);
        }
    }

    public byte[]? ReadFrame(int frameSamples)
    {
        if (!_started)
            throw new InvalidOperationException("Producer is not started.");

        int requiredShorts = frameSamples * Channels;
        short[] temp = new short[requiredShorts];

        int waitedMs = 0;
        while (_ringBuffer.Count < requiredShorts && !_stopping && !_captureCompleted && waitedMs < 20)
        {
            Thread.Sleep(2);
            waitedMs += 2;
        }

        int read = _ringBuffer.Read(temp, 0, requiredShorts);

        if ((_stopping || _captureCompleted) && read == 0)
            return null;

        if (read < requiredShorts)
        {
            Array.Clear(temp, read, requiredShorts - read);
        }

        byte[] payload = new byte[requiredShorts * sizeof(short)];
        Buffer.BlockCopy(temp, 0, payload, 0, payload.Length);
        return payload;
    }

    public void Stop()
    {
        lock (_stateSync)
        {
            if (!_started)
                return;

            _stopping = true;

            try
            {
                _captureCts?.Cancel();
            }
            catch
            {
            }
        }

        try
        {
            _captureTask?.Wait(1000);
        }
        catch
        {
        }

        try
        {
            _captureAdapter.Stop();
        }
        catch
        {
        }

        _captureTask = null;

        _captureCts?.Dispose();
        _captureCts = null;

        lock (_stateSync)
        {
            _started = false;
        }
    }

    private void CaptureLoop(CancellationToken cancellationToken)
    {
        float[] inputFloat = new float[_captureChunkSamples];
        short[] normalizedShorts = new short[_captureChunkSamples];

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int read = _captureAdapter.ReadSamples(inputFloat, 0, inputFloat.Length);

                if (read == 0)
                {
                    _captureCompleted = true;
                    break;
                }

                if (read < 0)
                {
                    _captureCompleted = true;
                    break;
                }

                int converted = _normalizer.ConvertToPcm16(
                    inputFloat,
                    read,
                    normalizedShorts,
                    0,
                    normalizedShorts.Length);

                if (converted <= 0)
                {
                    Thread.Sleep(1);
                    continue;
                }

                int written = _ringBuffer.Write(normalizedShorts, 0, converted);

                if (written < converted)
                {
                    Thread.Sleep(2);
                }

                while (_ringBuffer.Count > _bufferTargetSamples * 4 && !cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(1);
                }
            }
        }
        catch
        {
            _captureCompleted = true;
        }
        finally
        {
            _captureCompleted = true;
        }
    }

    public void Dispose()
    {
        Stop();
        _captureAdapter.Dispose();
    }
}