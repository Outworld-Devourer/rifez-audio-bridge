#pragma once

#include <ntddk.h>

typedef struct _RIFEZ_PCM_RING_BUFFER
{
    BYTE* Buffer;
    ULONG CapacityBytes;
    ULONG ReadOffset;
    ULONG WriteOffset;
    ULONG BufferedBytes;
    KSPIN_LOCK Lock;
    BOOLEAN Initialized;
} RIFEZ_PCM_RING_BUFFER, * PRIFEZ_PCM_RING_BUFFER;

extern RIFEZ_PCM_RING_BUFFER g_RifeZPcmBuffer;

NTSTATUS
RifeZPcmBufferInitialize(
    _Inout_ PRIFEZ_PCM_RING_BUFFER RingBuffer,
    _In_ ULONG CapacityBytes
);

void
RifeZPcmBufferReset(
    _Inout_ PRIFEZ_PCM_RING_BUFFER RingBuffer
);

void
RifeZPcmBufferCleanup(
    _Inout_ PRIFEZ_PCM_RING_BUFFER RingBuffer
);

void
RifeZPcmBufferWrite(
    _Inout_ PRIFEZ_PCM_RING_BUFFER RingBuffer,
    _In_reads_bytes_(ByteCount) const BYTE* Data,
    _In_ ULONG ByteCount
);

NTSTATUS
RifeZPcmBufferRead(
    _Inout_ PRIFEZ_PCM_RING_BUFFER RingBuffer,
    _Out_writes_bytes_all_(BufferCapacity) BYTE* Buffer,
    _In_ ULONG BufferCapacity,
    _Out_ PULONG BytesRead
);

ULONG
RifeZPcmBufferGetBufferedBytes(
    _Inout_ PRIFEZ_PCM_RING_BUFFER RingBuffer
);

ULONG
RifeZPcmBufferGetCapacityBytes(
    _In_ PRIFEZ_PCM_RING_BUFFER RingBuffer
);