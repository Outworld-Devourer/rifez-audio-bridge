#include <sysvad.h>
#include "rifezcontroldevice.h"
#include "rifezioctl.h"
#include "rifezpcmservice.h"

#pragma warning(disable:4127)

PDEVICE_OBJECT g_RifeZControlDeviceObject = NULL;

PDRIVER_DISPATCH g_RifeZOriginalCreateDispatch = NULL;
PDRIVER_DISPATCH g_RifeZOriginalCloseDispatch = NULL;
PDRIVER_DISPATCH g_RifeZOriginalDeviceControlDispatch = NULL;

static UNICODE_STRING g_RifeZDeviceName = RTL_CONSTANT_STRING(L"\\Device\\RifeZAudioBridge");
static UNICODE_STRING g_RifeZDosName = RTL_CONSTANT_STRING(L"\\DosDevices\\RifeZAudioBridge");

static
NTSTATUS
RifeZCompleteRequest(
    _In_ PIRP Irp,
    _In_ NTSTATUS Status,
    _In_ ULONG_PTR Information
)
{
    Irp->IoStatus.Status = Status;
    Irp->IoStatus.Information = Information;
    IoCompleteRequest(Irp, IO_NO_INCREMENT);
    return Status;
}

static
BOOLEAN
RifeZIsControlDevice(
    _In_ PDEVICE_OBJECT DeviceObject
)
{
    return (DeviceObject != NULL && DeviceObject == g_RifeZControlDeviceObject);
}

NTSTATUS
RifeZControlDeviceCreate(
    _In_ PDRIVER_OBJECT DriverObject
)
{
    PAGED_CODE();

    if (g_RifeZControlDeviceObject != NULL)
    {
        return STATUS_SUCCESS;
    }

    NTSTATUS ntStatus = IoCreateDevice(
        DriverObject,
        0,
        &g_RifeZDeviceName,
        FILE_DEVICE_UNKNOWN,
        FILE_DEVICE_SECURE_OPEN,
        FALSE,
        &g_RifeZControlDeviceObject);

    if (!NT_SUCCESS(ntStatus))
    {
        DPF(D_ERROR, ("RifeZ: IoCreateDevice failed, 0x%x", ntStatus));
        g_RifeZControlDeviceObject = NULL;
        return ntStatus;
    }

    g_RifeZControlDeviceObject->Flags |= DO_BUFFERED_IO;
    g_RifeZControlDeviceObject->Flags &= ~DO_DEVICE_INITIALIZING;

    ntStatus = IoCreateSymbolicLink(&g_RifeZDosName, &g_RifeZDeviceName);
    if (!NT_SUCCESS(ntStatus))
    {
        DPF(D_ERROR, ("RifeZ: IoCreateSymbolicLink failed, 0x%x", ntStatus));
        IoDeleteDevice(g_RifeZControlDeviceObject);
        g_RifeZControlDeviceObject = NULL;
        return ntStatus;
    }

    DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "[RifeZ] control device created\n");
    return STATUS_SUCCESS;
}

void
RifeZControlDeviceDelete()
{
    PAGED_CODE();

    IoDeleteSymbolicLink(&g_RifeZDosName);

    if (g_RifeZControlDeviceObject != NULL)
    {
        IoDeleteDevice(g_RifeZControlDeviceObject);
        g_RifeZControlDeviceObject = NULL;
    }
}

NTSTATUS
RifeZDispatchCreate(
    _In_ PDEVICE_OBJECT DeviceObject,
    _Inout_ PIRP Irp
)
{
    if (RifeZIsControlDevice(DeviceObject))
    {
        return RifeZCompleteRequest(Irp, STATUS_SUCCESS, 0);
    }

    if (g_RifeZOriginalCreateDispatch != NULL)
    {
        return g_RifeZOriginalCreateDispatch(DeviceObject, Irp);
    }

    return RifeZCompleteRequest(Irp, STATUS_INVALID_DEVICE_REQUEST, 0);
}

NTSTATUS
RifeZDispatchClose(
    _In_ PDEVICE_OBJECT DeviceObject,
    _Inout_ PIRP Irp
)
{
    if (RifeZIsControlDevice(DeviceObject))
    {
        return RifeZCompleteRequest(Irp, STATUS_SUCCESS, 0);
    }

    if (g_RifeZOriginalCloseDispatch != NULL)
    {
        return g_RifeZOriginalCloseDispatch(DeviceObject, Irp);
    }

    return RifeZCompleteRequest(Irp, STATUS_INVALID_DEVICE_REQUEST, 0);
}

NTSTATUS
RifeZDispatchDeviceControl(
    _In_ PDEVICE_OBJECT DeviceObject,
    _Inout_ PIRP Irp
)
{
    if (!RifeZIsControlDevice(DeviceObject))
    {
        if (g_RifeZOriginalDeviceControlDispatch != NULL)
        {
            return g_RifeZOriginalDeviceControlDispatch(DeviceObject, Irp);
        }

        return RifeZCompleteRequest(Irp, STATUS_INVALID_DEVICE_REQUEST, 0);
    }

    PIO_STACK_LOCATION irpSp = IoGetCurrentIrpStackLocation(Irp);
    if (irpSp == NULL)
    {
        return RifeZCompleteRequest(Irp, STATUS_INVALID_DEVICE_REQUEST, 0);
    }

    ULONG ioControlCode = irpSp->Parameters.DeviceIoControl.IoControlCode;
    PVOID systemBuffer = Irp->AssociatedIrp.SystemBuffer;
    ULONG outputLength = irpSp->Parameters.DeviceIoControl.OutputBufferLength;

    switch (ioControlCode)
    {
    case IOCTL_RIFEZ_GET_PCM_INFO:
    {
        if (systemBuffer == NULL || outputLength < sizeof(RIFEZ_PCM_INFO))
        {
            return RifeZCompleteRequest(Irp, STATUS_BUFFER_TOO_SMALL, 0);
        }

        PRIFEZ_PCM_INFO info = (PRIFEZ_PCM_INFO)systemBuffer;
        RifeZPcmServiceGetInfo(info);

        return RifeZCompleteRequest(Irp, STATUS_SUCCESS, sizeof(RIFEZ_PCM_INFO));
    }

    case IOCTL_RIFEZ_READ_PCM:
    {
        if (systemBuffer == NULL || outputLength == 0)
        {
            return RifeZCompleteRequest(Irp, STATUS_BUFFER_TOO_SMALL, 0);
        }

        ULONG bytesRead = 0;
        NTSTATUS ntStatus = RifeZPcmServiceRead(
            (BYTE*)systemBuffer,
            outputLength,
            &bytesRead);

        if (ntStatus == STATUS_DEVICE_NOT_READY)
        {
            return RifeZCompleteRequest(Irp, STATUS_SUCCESS, 0);
        }

        if (!NT_SUCCESS(ntStatus))
        {
            return RifeZCompleteRequest(Irp, ntStatus, 0);
        }

        return RifeZCompleteRequest(Irp, STATUS_SUCCESS, bytesRead);
    }
    case IOCTL_RIFEZ_RESET_PCM:
    {
        RifeZPcmServiceReset();
        return RifeZCompleteRequest(Irp, STATUS_SUCCESS, 0);
    }

    default:
        return RifeZCompleteRequest(Irp, STATUS_INVALID_DEVICE_REQUEST, 0);
    }
}