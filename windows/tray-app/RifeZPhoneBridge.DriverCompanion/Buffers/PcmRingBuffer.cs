namespace RifeZPhoneBridge.DriverCompanion.Buffers;

public sealed class PcmRingBuffer
{
    private readonly short[] _buffer;
    private readonly object _sync = new();

    private int _readIndex;
    private int _writeIndex;
    private int _count;

    public int Capacity => _buffer.Length;

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _count;
            }
        }
    }

    public PcmRingBuffer(int capacitySamples)
    {
        if (capacitySamples <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacitySamples));

        _buffer = new short[capacitySamples];
    }

    public int Write(short[] source, int offset, int count)
    {
        if (count <= 0)
            return 0;

        lock (_sync)
        {
            int writable = Math.Min(count, _buffer.Length - _count);
            int remaining = writable;
            int srcIndex = offset;

            while (remaining > 0)
            {
                int chunk = Math.Min(remaining, _buffer.Length - _writeIndex);
                Array.Copy(source, srcIndex, _buffer, _writeIndex, chunk);

                _writeIndex = (_writeIndex + chunk) % _buffer.Length;
                _count += chunk;
                srcIndex += chunk;
                remaining -= chunk;
            }

            return writable;
        }
    }

    public int Read(short[] destination, int offset, int count)
    {
        if (count <= 0)
            return 0;

        lock (_sync)
        {
            int readable = Math.Min(count, _count);
            int remaining = readable;
            int dstIndex = offset;

            while (remaining > 0)
            {
                int chunk = Math.Min(remaining, _buffer.Length - _readIndex);
                Array.Copy(_buffer, _readIndex, destination, dstIndex, chunk);

                _readIndex = (_readIndex + chunk) % _buffer.Length;
                _count -= chunk;
                dstIndex += chunk;
                remaining -= chunk;
            }

            return readable;
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            _readIndex = 0;
            _writeIndex = 0;
            _count = 0;
        }
    }
}