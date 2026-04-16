using RifeZPhoneBridge.Host.Models;

namespace RifeZPhoneBridge.App;

public sealed class BridgeAppSettings
{
    public string ClientName { get; set; } = "RifeZ-Windows-Bridge";

    public string? ManualHost { get; set; }
    public int ManualPort { get; set; } = 49521;

    public string? LastReceiverHost { get; set; }
    public int LastReceiverPort { get; set; } = 49521;
    public string? LastReceiverName { get; set; }
    public DateTimeOffset? LastReceiverSeenUtc { get; set; }

    public int FlatBufferHelloPort { get; set; } = 49522;
    public int AudioPcmPort { get; set; } = 49523;

    public int SampleRate { get; set; } = 48000;
    public int Channels { get; set; } = 2;
    public int FrameSamples { get; set; } = 480;
    public int StartupBurstFrames { get; set; } = 0;

    public AudioInputKind InputKind { get; set; } = AudioInputKind.Driver;
    public string? InputSourcePath { get; set; }

    public bool AutoConnectOnLaunch { get; set; } = true;
    public bool AutoStartStreaming { get; set; } = true;
    public bool RunAtWindowsStartup { get; set; } = false;
}