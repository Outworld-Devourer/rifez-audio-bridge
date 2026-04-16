using RifeZPhoneBridge.Core.Discovery;
using RifeZPhoneBridge.Host.Abstractions;
using RifeZPhoneBridge.Host.Models;
using RifeZPhoneBridge.Host.Services;

const string defaultHost = "192.168.1.3";
const int defaultPort = 49521;

string manualHost = defaultHost;
int manualPort = defaultPort;

AudioInputKind inputKind = AudioInputKind.Loopback;
string? inputSourcePath = null;

foreach (string arg in args)
{
    if (arg.StartsWith("--host=", StringComparison.OrdinalIgnoreCase))
    {
        manualHost = arg.Split('=', 2)[1].Trim();
    }
    else if (arg.StartsWith("--port=", StringComparison.OrdinalIgnoreCase))
    {
        if (int.TryParse(arg.Split('=', 2)[1].Trim(), out int parsedPort))
        {
            manualPort = parsedPort;
        }
    }
    else if (arg.StartsWith("--input=", StringComparison.OrdinalIgnoreCase))
    {
        string mode = arg.Split('=', 2)[1].Trim().ToLowerInvariant();

        inputKind = mode switch
        {
            "loopback" => AudioInputKind.Loopback,
            "driver" => AudioInputKind.Driver,
            "wav" => AudioInputKind.Wav,
            _ => throw new InvalidOperationException($"Unknown input kind: {mode}")
        };
    }
    else if (arg.StartsWith("--input-path=", StringComparison.OrdinalIgnoreCase))
    {
        inputSourcePath = arg.Split('=', 2)[1].Trim();
    }
}

Console.WriteLine("Raw args: " + string.Join(" ", args));
Console.WriteLine($"Parsed input kind variable: {inputKind}");

var options = new BridgeHostOptions
{
    ClientName = "RifeZ-Windows-Bridge",
    ManualHost = manualHost,
    ManualPort = manualPort,
    FlatBufferHelloPort = 49522,
    AudioPcmPort = 49523,
    SampleRate = 48000,
    Channels = 2,
    FrameSamples = 480,
    StartupBurstFrames = 24,
    InputKind = inputKind,
    InputSourcePath = inputSourcePath
};

Console.WriteLine($"Options input kind: {options.InputKind}");
Console.WriteLine($"Input kind: {options.InputKind}");
if (!string.IsNullOrWhiteSpace(options.InputSourcePath))
{
    Console.WriteLine($"Input source path: {options.InputSourcePath}");
}
if (options.InputKind == AudioInputKind.Driver)
{
    Console.WriteLine($"Driver PCM ingress pipe: {DriverIngressDefaults.DefaultPipeName}");
}

var discovery = new ZeroconfReceiverDiscoveryService();

await using var receiverSessionManager = new ReceiverSessionManager(
    discovery,
    options.ClientName,
    options.FlatBufferHelloPort);

var coordinator = new AudioStreamingCoordinator(options.AudioPcmPort);
var inputProviderFactory = new DefaultAudioInputProviderFactory();

await using var bridgeHost = new BridgeHost(
    options,
    receiverSessionManager,
    coordinator,
    inputProviderFactory);

IBridgeCommandService commands = new BridgeCommandService(bridgeHost);

bridgeHost.StateChanged += state =>
{
    Console.WriteLine(
        $"[RUNTIME] State={state.State}" +
        $" | ReceiverHost={state.ReceiverHost ?? "NONE"}" +
        $" | ReceiverPort={(state.ReceiverPort?.ToString() ?? "NONE")}" +
        $" | LastError={state.LastError ?? "NONE"}");
};

using var exitCts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    exitCts.Cancel();
};

Console.WriteLine("RifeZ Audio Bridge Runtime");
Console.WriteLine($"Pipe: {NamedPipeHostCommandServer.DefaultPipeName}");
Console.WriteLine("Runtime started.");

await using var server = new NamedPipeHostCommandServer(
    commands,
    NamedPipeHostCommandServer.DefaultPipeName,
    exitHostCallback: () =>
    {
        exitCts.Cancel();
        return Task.CompletedTask;
    });

try
{
    await server.RunAsync(exitCts.Token);
}
catch (OperationCanceledException)
{
}
finally
{
    try
    {
        await commands.ShutdownAsync();
    }
    catch
    {
    }
}

Console.WriteLine("Runtime stopped.");