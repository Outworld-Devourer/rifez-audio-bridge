using System.Collections.Generic;
using System.Threading;
using NAudio.Wave;

namespace RifeZPhoneBridge.Core.Audio;

public sealed class WasapiLoopbackPcmSource : IPcmFrameSource
{
    private readonly WasapiLoopbackCapture _capture;
    private readonly object _lock = new();
    private readonly AutoResetEvent _dataAvailable = new(false);

    private readonly List<byte> _pcm16Buffer = new();

    private volatile bool _started;
    private volatile bool _disposed;
    private volatile Exception? _captureException;

    public int SampleRate { get; }
    public int Channels { get; }

    public WasapiLoopbackPcmSource(
        int sampleRate = 48000,
        int channels = 2)
    {
        SampleRate = sampleRate;
        Channels = channels;

        _capture = new WasapiLoopbackCapture();
        _capture.DataAvailable += OnDataAvailable;
        _capture.RecordingStopped += OnRecordingStopped;
    }

    public byte[]? ReadFrame(int frameSamples)
    {
        ThrowIfDisposed();
        EnsureStarted();

        int requiredBytes = frameSamples * Channels * sizeof(short);

        while (true)
        {
            if (_captureException is not null)
                throw new InvalidOperationException("WASAPI loopback capture failed.", _captureException);

            lock (_lock)
            {
                if (_pcm16Buffer.Count >= requiredBytes)
                {
                    // If buffer got too large, drop the oldest audio and keep only recent audio.
                    if (_pcm16Buffer.Count > MAX_LIVE_BUFFER_BYTES)
                    {
                        int targetKeep = Math.Max(requiredBytes * 2, MAX_LIVE_BUFFER_BYTES / 2);
                        int dropBytes = _pcm16Buffer.Count - targetKeep;
                        if (dropBytes > 0)
                        {
                            _pcm16Buffer.RemoveRange(0, dropBytes);
                        }
                    }

                    byte[] frame = _pcm16Buffer.GetRange(0, requiredBytes).ToArray();
                    _pcm16Buffer.RemoveRange(0, requiredBytes);
                    return frame;
                }
            }

            _dataAvailable.WaitOne(20);
        }
    }

    private void EnsureStarted()
    {
        if (_started) return;
        _capture.StartRecording();
        _started = true;
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
            _captureException = e.Exception;

        _dataAvailable.Set();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_disposed || e.BytesRecorded <= 0)
            return;

        try
        {
            var format = _capture.WaveFormat;

            if (format.SampleRate != SampleRate || format.Channels != Channels)
            {
                throw new InvalidOperationException(
                    $"Loopback format mismatch. Expected {SampleRate} Hz / {Channels} ch, got {format.SampleRate} Hz / {format.Channels} ch.");
            }

            byte[] converted = format.Encoding switch
            {
                WaveFormatEncoding.IeeeFloat when format.BitsPerSample == 32
                    => ConvertFloat32ToPcm16(e.Buffer, e.BytesRecorded),

                WaveFormatEncoding.Pcm when format.BitsPerSample == 16
                    => CopyExact(e.Buffer, e.BytesRecorded),

                _ => throw new InvalidOperationException(
                    $"Unsupported loopback format: {format.Encoding}, {format.BitsPerSample} bits")
            };

            lock (_lock)
            {
                _pcm16Buffer.AddRange(converted);

                // Hard clamp live buffer so latency cannot grow without bound.
                if (_pcm16Buffer.Count > HARD_MAX_BUFFER_BYTES)
                {
                    int overflow = _pcm16Buffer.Count - HARD_MAX_BUFFER_BYTES;
                    _pcm16Buffer.RemoveRange(0, overflow);
                }
            }

            _dataAvailable.Set();
        }
        catch (Exception ex)
        {
            _captureException = ex;
            _dataAvailable.Set();
        }
    }

    private static byte[] CopyExact(byte[] source, int count)
    {
        byte[] result = new byte[count];
        Buffer.BlockCopy(source, 0, result, 0, count);
        return result;
    }

    private static byte[] ConvertFloat32ToPcm16(byte[] source, int bytesRecorded)
    {
        int floatCount = bytesRecorded / sizeof(float);
        byte[] result = new byte[floatCount * sizeof(short)];

        int outIndex = 0;
        for (int i = 0; i < bytesRecorded; i += 4)
        {
            float sample = BitConverter.ToSingle(source, i);

            if (sample > 1.0f) sample = 1.0f;
            if (sample < -1.0f) sample = -1.0f;

            short pcm16 = (short)Math.Round(sample * short.MaxValue);

            result[outIndex++] = (byte)(pcm16 & 0xFF);
            result[outIndex++] = (byte)((pcm16 >> 8) & 0xFF);
        }

        return result;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WasapiLoopbackPcmSource));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (_started)
                _capture.StopRecording();
        }
        catch
        {
        }

        _capture.DataAvailable -= OnDataAvailable;
        _capture.RecordingStopped -= OnRecordingStopped;
        _capture.Dispose();
        _dataAvailable.Dispose();
    }

    // Around 200 ms at 48k stereo PCM16
    private const int MAX_LIVE_BUFFER_BYTES = 48000 * 2 * 2 / 5;

    // Around 400 ms hard clamp
    private const int HARD_MAX_BUFFER_BYTES = 48000 * 2 * 2 / 2;
}