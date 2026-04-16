using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RifeZPhoneBridge.Core.Models;
using RifeZPhoneBridge.Core.Networking;
using RifeZPhoneBridge.Core.Protocol;

namespace RifeZPhoneBridge.Core.Services;

public sealed class PhoneSessionService : IAsyncDisposable
{
    private readonly TcpPhoneClient _client = new();

    public bool IsConnected => _client.IsConnected;

    public async Task ConnectAsync(PhoneEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        await _client.ConnectAsync(endpoint.Host, endpoint.Port, cancellationToken);
    }

    public async Task<string> HelloAsync(string clientName, CancellationToken cancellationToken = default)
    {
        return await _client.HelloAsync(clientName, cancellationToken);
    }

    public async Task<string> StartStreamAsync(CancellationToken cancellationToken = default)
    {
        return await _client.StreamStartAsync(cancellationToken);
    }

    public async Task<string> SendUnknownAsync(string rawCommand, CancellationToken cancellationToken = default)
    {
        return await _client.SendCommandAsync(rawCommand, cancellationToken);
    }

    public async Task<string> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        return await _client.DisconnectAsync(cancellationToken);
    }
    public async Task<string> PingAsync(CancellationToken cancellationToken = default)
    {
        return await _client.PingAsync(cancellationToken);
    }

    public async Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await _client.GetStatusAsync(cancellationToken);
    }

    public static bool IsOkResponse(string response) => ReceiverResponses.IsOk(response);
    public static bool IsErrorResponse(string response) => ReceiverResponses.IsError(response);

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }
}