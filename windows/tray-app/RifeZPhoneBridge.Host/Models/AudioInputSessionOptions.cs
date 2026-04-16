namespace RifeZPhoneBridge.Host.Models;

public sealed record AudioInputSessionOptions(
    AudioInputKind InputKind,
    int SampleRate,
    int Channels,
    string? SourcePath = null
);