using RifeZPhoneBridge.Host.Services;

if (args.Length == 0)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  --cmd=status");
    Console.WriteLine("  --cmd=init");
    Console.WriteLine("  --cmd=start");
    Console.WriteLine("  --cmd=stop");
    Console.WriteLine("  --cmd=shutdown");
    Console.WriteLine("  --cmd=exit-host");
    Console.WriteLine("  --driver-test-tone");
    return;
}

if (args.Any(a => a.Equals("--driver-test-tone", StringComparison.OrdinalIgnoreCase)))
{
    try
    {
        Console.WriteLine("Sending tone into driver PCM ingress pipe...");
        await DriverPipePcmWriterClient.SendToneAsync();
        Console.WriteLine("Driver ingress tone complete.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR|{ex.Message}");
    }

    return;
}

string? cmdArg = args.FirstOrDefault(a =>
    a.StartsWith("--cmd=", StringComparison.OrdinalIgnoreCase));

if (cmdArg is null)
{
    Console.WriteLine("ERROR|Missing --cmd=<command>");
    return;
}

string command = cmdArg.Split('=', 2)[1].Trim();

try
{
    string response = await NamedPipeHostCommandClient.SendAsync(command);
    Console.WriteLine(response);
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR|{ex.Message}");
}