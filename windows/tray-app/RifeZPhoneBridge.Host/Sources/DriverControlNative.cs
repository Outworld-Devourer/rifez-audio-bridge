using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace RifeZPhoneBridge.Host.Sources;

internal static class DriverControlNative
{
    internal const uint GenericRead = 0x80000000;
    internal const uint GenericWrite = 0x40000000;
    internal const uint FileShareRead = 0x00000001;
    internal const uint FileShareWrite = 0x00000002;
    internal const uint OpenExisting = 3;

    internal const uint FileDeviceRifezBridge = 0x00008337;
    internal const uint MethodBuffered = 0;
    internal const uint FileReadData = 0x0001;

    internal static uint CtlCode(uint deviceType, uint function, uint method, uint access) =>
        (deviceType << 16) | (access << 14) | (function << 2) | method;

    internal static readonly uint IoctlRifezGetPcmInfo =
        CtlCode(FileDeviceRifezBridge, 0x800, MethodBuffered, FileReadData);

    internal static readonly uint IoctlRifezReadPcm =
        CtlCode(FileDeviceRifezBridge, 0x801, MethodBuffered, FileReadData);

    internal static readonly uint IoctlRifezResetPcm =
    CtlCode(FileDeviceRifezBridge, 0x802, MethodBuffered, FileReadData);

    [StructLayout(LayoutKind.Sequential)]
    internal struct RifezPcmInfo
    {
        public uint SampleRate;
        public uint Channels;
        public uint BitsPerSample;
        public uint BufferCapacityBytes;
        public uint BufferedBytes;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        out RifezPcmInfo lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        [Out] byte[] lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);
}