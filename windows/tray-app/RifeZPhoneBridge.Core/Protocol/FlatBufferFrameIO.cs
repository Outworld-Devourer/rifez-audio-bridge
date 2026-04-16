using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Buffers.Binary;

namespace RifeZPhoneBridge.Core.Protocol;

public static class FlatBufferFrameIO
{
    public static async Task WriteFrameAsync(Stream stream, byte[] payload, CancellationToken cancellationToken = default)
    {
        byte[] header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);

        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static async Task<byte[]?> ReadFrameAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        byte[] header = new byte[4];
        int headerRead = await ReadExactlyAsync(stream, header, 0, 4, cancellationToken);
        if (headerRead == 0)
            return null;

        if (headerRead < 4)
            throw new IOException("Incomplete FlatBuffer frame header.");

        int length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length <= 0)
            throw new IOException($"Invalid FlatBuffer frame length: {length}");

        byte[] payload = new byte[length];
        int payloadRead = await ReadExactlyAsync(stream, payload, 0, length, cancellationToken);
        if (payloadRead < length)
            throw new IOException("Incomplete FlatBuffer frame payload.");

        return payload;
    }

    private static async Task<int> ReadExactlyAsync(
        Stream stream,
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        int totalRead = 0;

        while (totalRead < count)
        {
            int read = await stream.ReadAsync(
                buffer.AsMemory(offset + totalRead, count - totalRead),
                cancellationToken);

            if (read == 0)
                return totalRead;

            totalRead += read;
        }

        return totalRead;
    }
}