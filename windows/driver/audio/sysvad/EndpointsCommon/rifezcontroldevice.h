#pragma once

#include <ntddk.h>

extern PDEVICE_OBJECT g_RifeZControlDeviceObject;

extern PDRIVER_DISPATCH g_RifeZOriginalCreateDispatch;
extern PDRIVER_DISPATCH g_RifeZOriginalCloseDispatch;
extern PDRIVER_DISPATCH g_RifeZOriginalDeviceControlDispatch;

NTSTATUS
RifeZControlDeviceCreate(
    _In_ PDRIVER_OBJECT DriverObject
);

void
RifeZControlDeviceDelete();

NTSTATUS
RifeZDispatchCreate(
    _In_ PDEVICE_OBJECT DeviceObject,
    _Inout_ PIRP Irp
);

NTSTATUS
RifeZDispatchClose(
    _In_ PDEVICE_OBJECT DeviceObject,
    _Inout_ PIRP Irp
);

NTSTATUS
RifeZDispatchDeviceControl(
    _In_ PDEVICE_OBJECT DeviceObject,
    _Inout_ PIRP Irp
);