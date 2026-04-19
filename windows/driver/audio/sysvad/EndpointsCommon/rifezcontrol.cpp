#include <sysvad.h>
#include "rifezcontrol.h"
#include "rifezioctl.h"
#include "rifezpcmbuffer.h"

#define RIFEZ_CONTROL_POOLTAG 'LCRZ'
#pragma warning(disable:4127)
static PDEVICE_OBJECT g_RifeZControlDeviceObject = NULL;
static UNICODE_STRING g_RifeZDeviceName = RTL_CONSTANT_STRING(L"\\Device\\RifeZAudioBridge");
static UNICODE_STRING g_RifeZDosName = RTL_CONSTANT_STRING(L"\\DosDevices\\RifeZAudioBridge");

extern "C"
NTSTATUS
RifeZControlCreate(
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
        DPF(D_ERROR, ("RifeZControl: IoCreateDevice failed, 0x%x", ntStatus));
        g_RifeZControlDeviceObject = NULL;
        return ntStatus;
    }

    g_RifeZControlDeviceObject->Flags |= DO_BUFFERED_IO;

    ntStatus = IoCreateSymbolicLink(&g_RifeZDosName, &g_RifeZDeviceName);
    if (!NT_SUCCESS(ntStatus))
    {
        DPF(D_ERROR, ("RifeZControl: IoCreateSymbolicLink failed, 0x%x", ntStatus));
        IoDeleteDevice(g_RifeZControlDeviceObject);
        g_RifeZControlDeviceObject = NULL;
        return ntStatus;
    }

    g_RifeZControlDeviceObject->Flags &= ~DO_DEVICE_INITIALIZING;

    DPF(D_TERSE, ("RifeZControl: created \\Device\\RifeZAudioBridge"));
    return STATUS_SUCCESS;
}

extern "C"
void
RifeZControlDelete()
{
    PAGED_CODE();

    IoDeleteSymbolicLink(&g_RifeZDosName);

    if (g_RifeZControlDeviceObject != NULL)
    {
        IoDeleteDevice(g_RifeZControlDeviceObject);
        g_RifeZControlDeviceObject = NULL;
    }
}

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

#pragma code_seg()
NTSTATUS
RifeZControlCreateClose(
    _In_ PDEVICE_OBJECT DeviceObject,
    _Inout_ PIRP Irp
)
{
    UNREFERENCED_PARAMETER(DeviceObject);
    return RifeZCompleteRequest(Irp, STATUS_SUCCESS, 0);
}

#pragma code_seg()
NTSTATUS
RifeZControlDeviceControl(
    _In_ PDEVICE_OBJECT DeviceObject,
    _Inout_ PIRP Irp
)
{
    UNREFERENCED_PARAMETER(DeviceObject);

    PIO_STACK_LOCATION irpSp = IoGetCurrentIrpStackLocation(Irp);
    if (irpSp == NULL)
    {
        return RifeZCompleteRequest(Irp, STATUS_INVALID_DEVICE_REQUEST, 0);
    }

    ULONG ioControlCode = irpSp->Parameters.DeviceIoControl.IoControlCode;
    PVOID systemBuffer = Irp->AssociatedIrp.SystemBuffer;
    ULONG inputLength = irpSp->Parameters.DeviceIoControl.InputBufferLength;
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
        RtlZeroMemory(info, sizeof(RIFEZ_PCM_INFO));

        info->SampleRate = 48000;
        info->Channels = 2;
        info->BitsPerSample = 16;
        info->BufferCapacityBytes = RifeZPcmBufferGetCapacityBytes(&g_RifeZPcmBuffer);
        info->BufferedBytes = RifeZPcmBufferGetBufferedBytes(&g_RifeZPcmBuffer);

        return RifeZCompleteRequest(Irp, STATUS_SUCCESS, sizeof(RIFEZ_PCM_INFO));
    }

    case IOCTL_RIFEZ_READ_PCM:
    {
        UNREFERENCED_PARAMETER(inputLength);

        if (systemBuffer == NULL || outputLength == 0)
        {
            return RifeZCompleteRequest(Irp, STATUS_BUFFER_TOO_SMALL, 0);
        }

        ULONG bytesRead = 0;
        NTSTATUS ntStatus = RifeZPcmBufferRead(
            &g_RifeZPcmBuffer,
            (BYTE*)systemBuffer,
            outputLength,
            &bytesRead);

        if (!NT_SUCCESS(ntStatus) && ntStatus != STATUS_DEVICE_NOT_READY)
        {
            return RifeZCompleteRequest(Irp, ntStatus, 0);
        }

        return RifeZCompleteRequest(Irp, STATUS_SUCCESS, bytesRead);
    }

    default:
        return RifeZCompleteRequest(Irp, STATUS_INVALID_DEVICE_REQUEST, 0);
    }
}