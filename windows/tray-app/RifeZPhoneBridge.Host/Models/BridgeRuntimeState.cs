using RifeZPhoneBridge.Core.Models;

namespace RifeZPhoneBridge.Host.Models;

public enum BridgeStreamState
{
    Idle,
    Discovering,
    WaitingForReceiver,
    ReceiverSelected,
    ControlConnected,
    StreamConfigured,
    Streaming,
    Stopping,
    Faulted
}

public sealed record BridgeRuntimeState(
    BridgeStreamState State,
    string? ReceiverName,
    string? ReceiverHost,
    int? ReceiverPort,
    string? LastError,
    PhoneEndpoint? SelectedEndpoint
);