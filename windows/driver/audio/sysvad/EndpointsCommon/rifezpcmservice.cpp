#include <sysvad.h>
#include "rifezpcmservice.h"
#include "rifezpcmbuffer.h"
#pragma warning(disable:4996)
#pragma warning(disable:4127)
static LONG g_RifeZPcmServiceInitialized = 0;

NTSTATUS
RifeZPcmServiceInitialize()
{
    if (InterlockedCompareExchange(&g_RifeZPcmServiceInitialized, 1, 0) == 0)
    {
        return RifeZPcmBufferInitialize(&g_RifeZPcmBuffer, 1024 * 1024);
    }

    return STATUS_SUCCESS;
}

void
RifeZPcmServiceCleanup()
{
    if (InterlockedCompareExchange(&g_RifeZPcmServiceInitialized, 0, 1) == 1)
    {
        RifeZPcmBufferCleanup(&g_RifeZPcmBuffer);
    }
}

void
RifeZPcmServiceReset()
{
    RifeZPcmBufferReset(&g_RifeZPcmBuffer);
}

void
RifeZPcmServiceWrite(
    _In_reads_bytes_(ByteCount) const BYTE* Data,
    _In_ ULONG ByteCount
)
{
    RifeZPcmBufferWrite(&g_RifeZPcmBuffer, Data, ByteCount);
}

NTSTATUS
RifeZPcmServiceRead(
    _Out_writes_bytes_(BufferCapacity) BYTE* Buffer,
    _In_ ULONG BufferCapacity,
    _Out_ PULONG BytesRead
)
{
    return RifeZPcmBufferRead(&g_RifeZPcmBuffer, Buffer, BufferCapacity, BytesRead);
}

void
RifeZPcmServiceGetInfo(
    _Out_ PRIFEZ_PCM_INFO Info
)
{
    if (Info == NULL)
    {
        return;
    }

    RtlZeroMemory(Info, sizeof(RIFEZ_PCM_INFO));
    Info->SampleRate = 48000;
    Info->Channels = 2;
    Info->BitsPerSample = 16;
    Info->BufferCapacityBytes = RifeZPcmBufferGetCapacityBytes(&g_RifeZPcmBuffer);
    Info->BufferedBytes = RifeZPcmBufferGetBufferedBytes(&g_RifeZPcmBuffer);
}