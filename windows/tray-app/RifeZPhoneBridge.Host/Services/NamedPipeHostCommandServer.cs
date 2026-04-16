using System.IO.Pipes;
using System.Text;
using RifeZPhoneBridge.Host.Abstractions;
using RifeZPhoneBridge.Host.Models;

namespace RifeZPhoneBridge.Host.Services;

public sealed class NamedPipeHostCommandServer : IHostCommandServer
{
    public const string DefaultPipeName = "RifeZPhoneBridgeHost";

    private readonly IBridgeCommandService _commands;
    private readonly string _pipeName;
    private readonly Func<Task>? _exitHostCallback;

    public NamedPipeHostCommandServer(
        IBridgeCommandService commands,
        string pipeName = DefaultPipeName,
        Func<Task>? exitHostCallback = null)
    {
        _commands = commands;
        _pipeName = pipeName;
        _exitHostCallback = exitHostCallback;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var server = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await server.WaitForConnectionAsync(cancellationToken);

            using var reader = new StreamReader(server, Encoding.UTF8, false, 1024, leaveOpen: true);
            using var writer = new StreamWriter(server, new UTF8Encoding(false), 1024, leaveOpen: true)
            {
                AutoFlush = true
            };

            string? request = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(request))
            {
                await writer.WriteLineAsync("ERROR|Empty command");
                continue;
            }

            HostCommandResult result = await ExecuteCommandAsync(request.Trim(), cancellationToken);
            await writer.WriteLineAsync(result.Response);
        }
    }

    private async Task<HostCommandResult> ExecuteCommandAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            switch (command.ToLowerInvariant())
            {
                case "init":
                    await _commands.InitializeAsync(cancellationToken);
                    return new HostCommandResult(true, "OK");

                case "start":
                    await _commands.StartAsync(cancellationToken);
                    return new HostCommandResult(true, "OK");

                case "stop":
                    await _commands.StopAsync(cancellationToken);
                    return new HostCommandResult(true, "OK");

                case "shutdown":
                    await _commands.ShutdownAsync(cancellationToken);
                    return new HostCommandResult(true, "OK");

                case "status":
                    var status = _commands.GetStatus();
                    return new HostCommandResult(
                        true,
                        $"STATUS|{status.State}|{status.ReceiverHost ?? "NONE"}|{(status.ReceiverPort?.ToString() ?? "NONE")}|{status.IsInitialized}|{status.IsStreaming}|{status.LastError ?? "NONE"}");

                case "exit-host":
                    await _commands.ShutdownAsync(cancellationToken);

                    if (_exitHostCallback is not null)
                    {
                        await _exitHostCallback();
                    }

                    return new HostCommandResult(true, "OK");

                default:
                    return new HostCommandResult(false, $"ERROR|Unknown command: {command}");
            }
        }
        catch (Exception ex)
        {
            return new HostCommandResult(false, $"ERROR|{ex.Message}");
        }
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}