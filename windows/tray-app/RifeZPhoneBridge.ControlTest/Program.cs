using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static class Program
{
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;

    private const uint FILE_DEVICE_RIFEZ_BRIDGE = 0x00008337;
    private const uint METHOD_BUFFERED = 0;
    private const uint FILE_READ_DATA = 0x0001;

    [StructLayout(LayoutKind.Sequential)]
    private struct RifezPcmInfo
    {
        public uint SampleRate;
        public uint Channels;
        public uint BitsPerSample;
        public uint BufferCapacityBytes;
        public uint BufferedBytes;
    }

    private static uint CTL_CODE(uint deviceType, uint function, uint method, uint access) =>
        (deviceType << 16) | (access << 14) | (function << 2) | method;

    private static readonly uint IOCTL_RIFEZ_GET_PCM_INFO =
        CTL_CODE(FILE_DEVICE_RIFEZ_BRIDGE, 0x800, METHOD_BUFFERED, FILE_READ_DATA);

    private static readonly uint IOCTL_RIFEZ_READ_PCM =
        CTL_CODE(FILE_DEVICE_RIFEZ_BRIDGE, 0x801, METHOD_BUFFERED, FILE_READ_DATA);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        out RifezPcmInfo lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        [Out] byte[] lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    public static int Main(string[] args)
    {
        string outputPath = args.Length > 0
            ? args[0]
            : Path.Combine(AppContext.BaseDirectory, "rifez_capture.wav");

        int durationSeconds = 8;
        if (args.Length > 1 && int.TryParse(args[1], out int parsedSeconds) && parsedSeconds > 0)
        {
            durationSeconds = parsedSeconds;
        }

        using var handle = CreateFile(
            @"\\.\RifeZPhoneBridge",
            GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            0,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            Console.WriteLine($"Open failed. Win32={error} ({new Win32Exception(error).Message})");
            return 1;
        }

        Console.WriteLine("Open OK.");

        bool ok = DeviceIoControl(
            handle,
            IOCTL_RIFEZ_GET_PCM_INFO,
            IntPtr.Zero,
            0,
            out var info,
            (uint)Marshal.SizeOf<RifezPcmInfo>(),
            out uint bytesReturned,
            IntPtr.Zero);

        if (!ok)
        {
            int error = Marshal.GetLastWin32Error();
            Console.WriteLine($"IOCTL_RIFEZ_GET_PCM_INFO failed. Win32={error} ({new Win32Exception(error).Message})");
            return 2;
        }

        Console.WriteLine($"IOCTL OK, bytes={bytesReturned}");
        Console.WriteLine($"Rate={info.SampleRate}");
        Console.WriteLine($"Channels={info.Channels}");
        Console.WriteLine($"Bits={info.BitsPerSample}");
        Console.WriteLine($"Capacity={info.BufferCapacityBytes}");
        Console.WriteLine($"Buffered={info.BufferedBytes}");
        Console.WriteLine($"Output={outputPath}");
        Console.WriteLine($"DurationSeconds={durationSeconds}");

        int blockAlign = checked((int)(info.Channels * (info.BitsPerSample / 8)));
        int avgBytesPerSec = checked((int)(info.SampleRate * info.Channels * (info.BitsPerSample / 8)));

        byte[] readBuffer = new byte[8192];
        long totalAudioBytes = 0;
        DateTime endTime = DateTime.UtcNow.AddSeconds(durationSeconds);

        using FileStream fs = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using BinaryWriter writer = new(fs);

        WriteWaveHeaderPlaceholder(writer, info.SampleRate, info.Channels, info.BitsPerSample);

        while (DateTime.UtcNow < endTime)
        {
            ok = DeviceIoControl(
                handle,
                IOCTL_RIFEZ_READ_PCM,
                IntPtr.Zero,
                0,
                readBuffer,
                (uint)readBuffer.Length,
                out bytesReturned,
                IntPtr.Zero);

            if (!ok)
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine($"IOCTL_RIFEZ_READ_PCM failed. Win32={error} ({new Win32Exception(error).Message})");
                return 3;
            }

            if (bytesReturned > 0)
            {
                int aligned = (int)(bytesReturned - (bytesReturned % (uint)blockAlign));
                if (aligned > 0)
                {
                    writer.Write(readBuffer, 0, aligned);
                    totalAudioBytes += aligned;
                }
            }
            else
            {
                System.Threading.Thread.Sleep(10);
            }
        }

        FinalizeWaveHeader(writer, totalAudioBytes, info.SampleRate, info.Channels, info.BitsPerSample);

        Console.WriteLine($"Done. Wrote {totalAudioBytes} audio bytes.");
        Console.WriteLine($"Saved WAV: {outputPath}");
        Console.WriteLine($"AvgBytesPerSec={avgBytesPerSec}");

        return 0;
    }

    private static void WriteWaveHeaderPlaceholder(
        BinaryWriter writer,
        uint sampleRate,
        uint channels,
        uint bitsPerSample)
    {
        uint byteRate = sampleRate * channels * (bitsPerSample / 8);
        ushort blockAlign = (ushort)(channels * (bitsPerSample / 8));

        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(0);
        writer.Write(new[] { 'W', 'A', 'V', 'E' });

        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((ushort)1);
        writer.Write((ushort)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write((ushort)bitsPerSample);

        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(0);
    }

    private static void FinalizeWaveHeader(
        BinaryWriter writer,
        long audioDataBytes,
        uint sampleRate,
        uint channels,
        uint bitsPerSample)
    {
        if (audioDataBytes > int.MaxValue)
        {
            throw new InvalidOperationException("WAV data too large for simple RIFF header.");
        }

        uint byteRate = sampleRate * channels * (bitsPerSample / 8);
        ushort blockAlign = (ushort)(channels * (bitsPerSample / 8));

        writer.Flush();
        Stream stream = writer.BaseStream;

        stream.Seek(0, SeekOrigin.Begin);

        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write((int)(36 + audioDataBytes));
        writer.Write(new[] { 'W', 'A', 'V', 'E' });

        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((ushort)1);
        writer.Write((ushort)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write((ushort)bitsPerSample);

        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write((int)audioDataBytes);

        writer.Flush();
    }
}