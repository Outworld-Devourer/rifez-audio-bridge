using System.IO.Pipes;
using System.Text;

internal static class NamedPipeHostCommandClient
{
    public static async Task<string> SendAsync(
        string command,
        string pipeName = "RifeZPhoneBridgeHost",
        CancellationToken cancellationToken = default)
    {
        using var client = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        await client.ConnectAsync(cancellationToken);

        using var writer = new StreamWriter(client, new UTF8Encoding(false), 1024, leaveOpen: true)
        {
            AutoFlush = true
        };

        using var reader = new StreamReader(client, Encoding.UTF8, false, 1024, leaveOpen: true);

        await writer.WriteLineAsync(command);
        string? response = await reader.ReadLineAsync();

        return response ?? "ERROR|No response";
    }
}