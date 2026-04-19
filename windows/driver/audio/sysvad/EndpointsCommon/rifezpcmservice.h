#pragma once

#include <ntddk.h>
#include "rifezioctl.h"

NTSTATUS
RifeZPcmServiceInitialize();

void
RifeZPcmServiceCleanup();

void
RifeZPcmServiceReset();

void
RifeZPcmServiceWrite(
    _In_reads_bytes_(ByteCount) const BYTE* Data,
    _In_ ULONG ByteCount
);

NTSTATUS
RifeZPcmServiceRead(
    _Out_writes_bytes_(BufferCapacity) BYTE* Buffer,
    _In_ ULONG BufferCapacity,
    _Out_ PULONG BytesRead
);

void
RifeZPcmServiceGetInfo(
    _Out_ PRIFEZ_PCM_INFO Info
);