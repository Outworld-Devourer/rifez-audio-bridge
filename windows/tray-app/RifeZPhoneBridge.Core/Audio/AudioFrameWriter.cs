using System.Buffers.Binary;

namespace RifeZPhoneBridge.Core.Audio;

public static class AudioFrameWriter
{
    public const byte FrameTypePcm16 = 1;

    public static async Task WritePcm16FrameAsync(
        Stream stream,
        byte[] pcmPayload,
        long presentationIndex,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(pcmPayload);

        byte[] header = new byte[13];
        header[0] = FrameTypePcm16;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(1, 4), pcmPayload.Length);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(5, 8), presentationIndex);

        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(pcmPayload, cancellationToken);
    }
}