namespace RifeZPhoneBridge.Host.Models;

public sealed record BridgeStatusSnapshot(
    BridgeStreamState State,
    string? ReceiverHost,
    int? ReceiverPort,
    string? LastError,
    bool IsInitialized,
    bool IsStreaming
);