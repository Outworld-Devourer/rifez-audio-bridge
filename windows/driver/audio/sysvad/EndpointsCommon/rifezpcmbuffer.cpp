#include <sysvad.h>
#include "rifezpcmbuffer.h"

#define RIFEZ_PCM_POOLTAG 'PCRZ'
#pragma warning(disable:4996)
#pragma warning(disable:4127)
RIFEZ_PCM_RING_BUFFER g_RifeZPcmBuffer = {};

NTSTATUS
RifeZPcmBufferInitialize(
    _Inout_ PRIFEZ_PCM_RING_BUFFER RingBuffer,
    _In_ ULONG CapacityBytes
)
{
    if (RingBuffer == NULL || CapacityBytes == 0)
    {
        return STATUS_INVALID_PARAMETER;
    }

    if (RingBuffer->Buffer != NULL)
    {
        ExFreePoolWithTag(RingBuffer->Buffer, RIFEZ_PCM_POOLTAG);
        RingBuffer->Buffer = NULL;
    }

    RingBuffer->Buffer = (BYTE*)ExAllocatePool2(
        POOL_FLAG_NON_PAGED,
        CapacityBytes,
        RIFEZ_PCM_POOLTAG);

    if (RingBuffer->Buffer == NULL)
    {
        RingBuffer->CapacityBytes = 0;
        RingBuffer->ReadOffset = 0;
        RingBuffer->WriteOffset = 0;
        RingBuffer->BufferedBytes = 0;
        RingBuffer->Initialized = FALSE;
        return STATUS_INSUFFICIENT_RESOURCES;
    }

    RingBuffer->CapacityBytes = CapacityBytes;
    RingBuffer->ReadOffset = 0;
    RingBuffer->WriteOffset = 0;
    RingBuffer->BufferedBytes = 0;
    RingBuffer->Initialized = TRUE;
    KeInitializeSpinLock(&RingBuffer->Lock);

    return STATUS_SUCCESS;
}

void
RifeZPcmBufferReset(
    _Inout_ PRIFEZ_PCM_RING_BUFFER RingBuffer
)
{
    if (RingBuffer == NULL || !RingBuffer->Initialized)
    {
        return;
    }

    KIRQL oldIrql;
    KeAcquireSpinLock(&RingBuffer->Lock, &oldIrql);

    RingBuffer->ReadOffset = 0;
    RingBuffer->WriteOffset = 0;
    RingBuffer->BufferedBytes = 0;

    KeReleaseSpinLock(&RingBuffer->Lock, oldIrql);
}

void
RifeZPcmBufferCleanup(
    _Inout_ PRIFEZ_PCM_RING_BUFFER RingBuffer
)
{
    if (RingBuffer == NULL)
    {
        return;
    }

    if (RingBuffer->Buffer != NULL)
    {
        ExFreePoolWithTag(RingBuffer->Buffer, RIFEZ_PCM_POOLTAG);
        RingBuffer->Buffer = NULL;
    }

    RingBuffer->CapacityBytes = 0;
    RingBuffer->ReadOffset = 0;
    RingBuffer->WriteOffset = 0;
    RingBuffer->BufferedBytes = 0;
    RingBuffer->Initialized = FALSE;
}

void
RifeZPcmBufferWrite(
    _Inout_ PRIFEZ_PCM_RING_BUFFER RingBuffer,
    _In_reads_bytes_(ByteCount) const BYTE* Data,
    _In_ ULONG ByteCount
)
{
    if (RingBuffer == NULL || !RingBuffer->Initialized || Data == NULL || ByteCount == 0)
    {
        return;
    }

    KIRQL oldIrql;
    KeAcquireSpinLock(&RingBuffer->Lock, &oldIrql);

    for (ULONG i = 0; i < ByteCount; ++i)
    {
        if (RingBuffer->BufferedBytes == RingBuffer->CapacityBytes)
        {
            RingBuffer->ReadOffset = (RingBuffer->ReadOffset + 1) % RingBuffer->CapacityBytes;
            RingBuffer->BufferedBytes--;
        }

        RingBuffer->Buffer[RingBuffer->WriteOffset] = Data[i];
        RingBuffer->WriteOffset = (RingBuffer->WriteOffset + 1) % RingBuffer->CapacityBytes;
        RingBuffer->BufferedBytes++;
    }

    KeReleaseSpinLock(&RingBuffer->Lock, oldIrql);
}

NTSTATUS
RifeZPcmBufferRead(
    _Inout_ PRIFEZ_PCM_RING_BUFFER RingBuffer,
    _Out_writes_bytes_all_(BufferCapacity) BYTE* Buffer,
    _In_ ULONG BufferCapacity,
    _Out_ PULONG BytesRead
)
{
    if (BytesRead == NULL)
    {
        return STATUS_INVALID_PARAMETER;
    }

    *BytesRead = 0;

    if (Buffer == NULL || BufferCapacity == 0)
    {
        return STATUS_INVALID_PARAMETER;
    }

    RtlZeroMemory(Buffer, BufferCapacity);

    if (RingBuffer == NULL || !RingBuffer->Initialized)
    {
        return STATUS_DEVICE_NOT_READY;
    }

    KIRQL oldIrql;
    KeAcquireSpinLock(&RingBuffer->Lock, &oldIrql);

    ULONG bytesToRead = (RingBuffer->BufferedBytes < BufferCapacity)
        ? RingBuffer->BufferedBytes
        : BufferCapacity;

    for (ULONG i = 0; i < bytesToRead; ++i)
    {
        Buffer[i] = RingBuffer->Buffer[RingBuffer->ReadOffset];
        RingBuffer->ReadOffset = (RingBuffer->ReadOffset + 1) % RingBuffer->CapacityBytes;
    }

    RingBuffer->BufferedBytes -= bytesToRead;
    *BytesRead = bytesToRead;

    KeReleaseSpinLock(&RingBuffer->Lock, oldIrql);

    return STATUS_SUCCESS;
}

ULONG
RifeZPcmBufferGetBufferedBytes(
    _Inout_ PRIFEZ_PCM_RING_BUFFER RingBuffer
)
{
    if (RingBuffer == NULL || !RingBuffer->Initialized)
    {
        return 0;
    }

    ULONG bufferedBytes;
    KIRQL oldIrql;
    KeAcquireSpinLock(&RingBuffer->Lock, &oldIrql);
    bufferedBytes = RingBuffer->BufferedBytes;
    KeReleaseSpinLock(&RingBuffer->Lock, oldIrql);

    return bufferedBytes;
}

ULONG
RifeZPcmBufferGetCapacityBytes(
    _In_ PRIFEZ_PCM_RING_BUFFER RingBuffer
)
{
    if (RingBuffer == NULL)
    {
        return 0;
    }

    return RingBuffer->CapacityBytes;
}