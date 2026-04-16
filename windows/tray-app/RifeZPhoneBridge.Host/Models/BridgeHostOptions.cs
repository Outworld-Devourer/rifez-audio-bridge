namespace RifeZPhoneBridge.Host.Models;

public sealed class BridgeHostOptions
{
    public string ClientName { get; init; } = "RifeZ-Windows-Bridge";

    public string? ManualHost { get; init; }
    public int ManualPort { get; init; } = 49521;

    public string? LastReceiverHost { get; init; }
    public int LastReceiverPort { get; init; } = 49521;

    public int FlatBufferHelloPort { get; init; } = 49522;
    public int AudioPcmPort { get; init; } = 49523;

    public int SampleRate { get; init; } = 48000;
    public int Channels { get; init; } = 2;
    public int FrameSamples { get; init; } = 480;
    public int StartupBurstFrames { get; init; } = 0;

    public AudioInputKind InputKind { get; set; } = AudioInputKind.Driver;
    public string? InputSourcePath { get; set; }
}