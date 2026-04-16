namespace RifeZPhoneBridge.Host.Models;

public sealed record HostCommandResult(
    bool Success,
    string Response
);