#pragma once

#include <ntddk.h>

extern "C"
NTSTATUS
RifeZControlCreate(
    _In_ PDRIVER_OBJECT DriverObject
);

extern "C"
void
RifeZControlDelete();

_Dispatch_type_(IRP_MJ_CREATE)
DRIVER_DISPATCH RifeZControlCreateClose;

_Dispatch_type_(IRP_MJ_CLOSE)
DRIVER_DISPATCH RifeZControlCreateClose;

_Dispatch_type_(IRP_MJ_DEVICE_CONTROL)
DRIVER_DISPATCH RifeZControlDeviceControl;