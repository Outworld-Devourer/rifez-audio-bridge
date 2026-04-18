using System.ComponentModel;
using Microsoft.Win32.SafeHandles;
using RifeZPhoneBridge.Core.Audio;

namespace RifeZPhoneBridge.Host.Sources;

public sealed class DriverControlPcmFrameSource : IPcmFrameSource
{
    private readonly object _sync = new();
    private readonly string _devicePath;
    private SafeFileHandle? _handle;
    private bool _disposed;

    public int SampleRate { get; }
    public int Channels { get; }

    public DriverControlPcmFrameSource(
        int expectedSampleRate,
        int expectedChannels,
        string devicePath = @"\\.\RifeZAudioBridge")
    {
        _devicePath = devicePath;

        _handle = DriverControlNative.CreateFile(
            _devicePath,
            DriverControlNative.GenericRead | DriverControlNative.GenericWrite,
            DriverControlNative.FileShareRead | DriverControlNative.FileShareWrite,
            IntPtr.Zero,
            DriverControlNative.OpenExisting,
            0,
            IntPtr.Zero);

        if (_handle.IsInvalid)
        {
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            throw new IOException(
                $"Failed to open driver control device '{_devicePath}'. Win32={error} ({new Win32Exception(error).Message})");
        }

        if (!DriverControlNative.DeviceIoControl(
                _handle,
                DriverControlNative.IoctlRifezGetPcmInfo,
                IntPtr.Zero,
                0,
                out var info,
                (uint)System.Runtime.InteropServices.Marshal.SizeOf<DriverControlNative.RifezPcmInfo>(),
                out uint _,
                IntPtr.Zero))
        {
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            throw new IOException(
                $"Failed to query PCM info from '{_devicePath}'. Win32={error} ({new Win32Exception(error).Message})");
        }
        ResetDriverBuffer();

        if (info.BitsPerSample != 16)
            throw new NotSupportedException($"Unsupported driver PCM bit depth: {info.BitsPerSample}");

        SampleRate = checked((int)info.SampleRate);
        Channels = checked((int)info.Channels);

        if (SampleRate != expectedSampleRate || Channels != expectedChannels)
        {
            throw new InvalidOperationException(
                $"Driver PCM format mismatch. Driver={SampleRate} Hz/{Channels} ch, expected={expectedSampleRate} Hz/{expectedChannels} ch");
        }
    }

    private void ResetDriverBuffer()
    {
        SafeFileHandle? handle = _handle;
        if (handle is null || handle.IsInvalid)
            return;

        bool ok = DriverControlNative.DeviceIoControl(
            handle,
            DriverControlNative.IoctlRifezResetPcm,
            IntPtr.Zero,
            0,
            Array.Empty<byte>(),
            0,
            out uint _,
            IntPtr.Zero);

        if (!ok)
        {
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            throw new IOException(
                $"Failed to reset driver PCM buffer for '{_devicePath}'. Win32={error} ({new Win32Exception(error).Message})");
        }
    }

    public byte[]? ReadFrame(int frameSamples)
    {
        lock (_sync)
        {
            if (_disposed)
                return null;

            SafeFileHandle? handle = _handle;
            if (handle is null || handle.IsInvalid)
                return null;

            int requestedBytes = checked(frameSamples * Channels * sizeof(short));
            byte[] buffer = new byte[requestedBytes];

            if (!DriverControlNative.DeviceIoControl(
                    handle,
                    DriverControlNative.IoctlRifezReadPcm,
                    IntPtr.Zero,
                    0,
                    buffer,
                    (uint)buffer.Length,
                    out uint bytesReturned,
                    IntPtr.Zero))
            {
                int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                throw new IOException(
                    $"Failed to read PCM from '{_devicePath}'. Win32={error} ({new Win32Exception(error).Message})");
            }

            if (bytesReturned == 0)
            {
                Thread.Sleep(10);
                return Array.Empty<byte>();
            }

            int returned = checked((int)bytesReturned);
            int blockAlign = Channels * sizeof(short);
            int aligned = returned - (returned % blockAlign);

            if (aligned <= 0)
                return Array.Empty<byte>();

            if (aligned == buffer.Length)
                return buffer;

            byte[] trimmed = new byte[aligned];
            Buffer.BlockCopy(buffer, 0, trimmed, 0, aligned);
            return trimmed;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _handle?.Dispose();
            }
            catch
            {
            }

            _handle = null;
        }
    }
}