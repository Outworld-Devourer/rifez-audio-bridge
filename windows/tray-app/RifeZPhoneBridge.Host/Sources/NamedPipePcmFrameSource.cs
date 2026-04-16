using System.Buffers.Binary;
using System.IO.Pipes;
using RifeZPhoneBridge.Core.Audio;
using RifeZPhoneBridge.Host.Models;

namespace RifeZPhoneBridge.Host.Sources;

public sealed class NamedPipePcmFrameSource : IPcmFrameSource
{
    private readonly string _pipeName;
    private readonly int _connectTimeoutMs;
    private readonly object _sync = new();

    private NamedPipeServerStream? _server;
    private Stream? _stream;
    private bool _connected;
    private bool _aborted;

    public int SampleRate { get; }
    public int Channels { get; }

    public NamedPipePcmFrameSource(
        int sampleRate,
        int channels,
        string pipeName = DriverIngressDefaults.DefaultPipeName,
        int connectTimeoutMs = DriverIngressDefaults.DefaultConnectTimeoutMs)
    {
        SampleRate = sampleRate;
        Channels = channels;
        _pipeName = pipeName;
        _connectTimeoutMs = connectTimeoutMs;
    }

    public byte[]? ReadFrame(int frameSamples)
    {
        if (_aborted)
            return null;

        EnsureConnected();

        if (_aborted)
            return null;

        Stream? stream;
        lock (_sync)
        {
            stream = _stream;
        }

        if (stream is null)
            return null;

        try
        {
            return ReadFrameFromStream(stream);
        }
        catch (ObjectDisposedException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public void Abort()
    {
        _aborted = true;

        lock (_sync)
        {
            try
            {
                _stream?.Dispose();
            }
            catch
            {
            }

            try
            {
                _server?.Dispose();
            }
            catch
            {
            }

            _stream = null;
            _server = null;
            _connected = false;
        }
    }

    private void EnsureConnected()
    {
        if (_connected || _aborted)
            return;

        NamedPipeServerStream server = new(
            _pipeName,
            PipeDirection.In,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

        lock (_sync)
        {
            if (_aborted)
            {
                server.Dispose();
                return;
            }

            _server = server;
        }

        try
        {
            using var cts = new CancellationTokenSource(_connectTimeoutMs);
            var waitTask = server.WaitForConnectionAsync(cts.Token);
            waitTask.GetAwaiter().GetResult();

            if (_aborted)
            {
                Abort();
                return;
            }

            lock (_sync)
            {
                _stream = server;
                _connected = true;
            }
        }
        catch (OperationCanceledException)
        {
            Abort();
            throw new TimeoutException(
                $"Timed out waiting for PCM ingress pipe client: {_pipeName}");
        }
        catch (ObjectDisposedException)
        {
            // aborted while waiting
        }
    }

    private static byte[]? ReadFrameFromStream(Stream stream)
    {
        byte[] header = new byte[13];

        if (!ReadExactly(stream, header, 0, header.Length))
            return null;

        byte frameType = header[0];
        if (frameType != AudioFrameWriter.FrameTypePcm16)
            throw new InvalidOperationException($"Unsupported frame type: {frameType}");

        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(1, 4));
        if (payloadLength < 0)
            throw new InvalidOperationException($"Invalid payload length: {payloadLength}");

        long presentationIndex = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(5, 8));
        _ = presentationIndex;

        byte[] payload = new byte[payloadLength];

        if (!ReadExactly(stream, payload, 0, payload.Length))
            return null;

        return payload;
    }

    private static bool ReadExactly(Stream stream, byte[] buffer, int offset, int count)
    {
        int totalRead = 0;

        while (totalRead < count)
        {
            int read = stream.Read(buffer, offset + totalRead, count - totalRead);
            if (read == 0)
                return false;

            totalRead += read;
        }

        return true;
    }

    public void Dispose()
    {
        Abort();
    }
}