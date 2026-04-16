using RifeZPhoneBridge.DriverCompanion;
using RifeZPhoneBridge.Host.Models;

string mode = "tone";
string? wavPath = null;
string pipeName = DriverIngressDefaults.DefaultPipeName;
int sampleRate = 48000;
int channels = 2;
int frameSamples = 480;
int? durationMs = null;

foreach (string arg in args)
{
    if (arg.StartsWith("--mode=", StringComparison.OrdinalIgnoreCase))
    {
        mode = arg.Split('=', 2)[1].Trim().ToLowerInvariant();
    }
    else if (arg.StartsWith("--wav=", StringComparison.OrdinalIgnoreCase))
    {
        wavPath = arg.Split('=', 2)[1].Trim();
    }
    else if (arg.StartsWith("--pipe=", StringComparison.OrdinalIgnoreCase))
    {
        pipeName = arg.Split('=', 2)[1].Trim();
    }
    else if (arg.StartsWith("--rate=", StringComparison.OrdinalIgnoreCase))
    {
        if (int.TryParse(arg.Split('=', 2)[1].Trim(), out int v))
            sampleRate = v;
    }
    else if (arg.StartsWith("--channels=", StringComparison.OrdinalIgnoreCase))
    {
        if (int.TryParse(arg.Split('=', 2)[1].Trim(), out int v))
            channels = v;
    }
    else if (arg.StartsWith("--frame=", StringComparison.OrdinalIgnoreCase))
    {
        if (int.TryParse(arg.Split('=', 2)[1].Trim(), out int v))
            frameSamples = v;
    }
    else if (arg.StartsWith("--duration-ms=", StringComparison.OrdinalIgnoreCase))
    {
        if (int.TryParse(arg.Split('=', 2)[1].Trim(), out int v) && v > 0)
            durationMs = v;
    }
}

Console.WriteLine("RifeZ Driver Companion");
Console.WriteLine($"Mode: {mode}");
Console.WriteLine($"Pipe: {pipeName}");
Console.WriteLine($"Rate: {sampleRate}");
Console.WriteLine($"Channels: {channels}");
Console.WriteLine($"FrameSamples: {frameSamples}");

if (durationMs.HasValue)
{
    Console.WriteLine($"DurationMs: {durationMs.Value}");
}

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

int? toneMaxFrames = null;

if (mode == "tone" && durationMs.HasValue)
{
    double frameDurationMs = frameSamples * 1000.0 / sampleRate;
    toneMaxFrames = Math.Max(1, (int)Math.Ceiling(durationMs.Value / frameDurationMs));
}

IAudioProducer producer = mode switch
{
    "tone" => new ToneProducer(sampleRate, channels, toneMaxFrames),
    "wav" => new WavProducer(
        string.IsNullOrWhiteSpace(wavPath)
            ? throw new InvalidOperationException("Missing --wav=<path> for wav mode.")
            : wavPath),
    "driver" => VirtualDeviceProducerPlaceholder.Create(sampleRate, channels, durationMs),
    "stdin" => throw new NotImplementedException("stdin mode is not implemented yet."),
    _ => throw new InvalidOperationException($"Unknown mode: {mode}")
};

await using var writer = new DriverPipeWriter(pipeName);

try
{
    await writer.ConnectAsync(cts.Token);
    Console.WriteLine("Connected to runtime ingress pipe.");

    await writer.SendProducerAsync(
        producer,
        frameSamples: frameSamples,
        cancellationToken: cts.Token);

    Console.WriteLine("PCM transmission completed.");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Canceled.");
}
finally
{
    producer.Dispose();
}