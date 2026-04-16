using RifeZPhoneBridge.Core.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using RifeZ.PhoneAudio.Control;



namespace RifeZPhoneBridge.Core.Networking;

public sealed class FlatBufferHelloClient : IAsyncDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(host, port, cancellationToken);
        _stream = _client.GetStream();
    }

    public async Task<string> HelloAsync(string clientName, CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        byte[] request = FlatBufferControlProtocol.BuildHelloRequest(clientName);
        await FlatBufferFrameIO.WriteFrameAsync(_stream!, request, cancellationToken);

        byte[]? response = await FlatBufferFrameIO.ReadFrameAsync(_stream!, cancellationToken);
        if (response is null)
            throw new IOException("Connection closed before FlatBuffer hello response.");

        return FlatBufferControlProtocol.ParseHelloResponse(response);
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        byte[] request = FlatBufferControlProtocol.BuildPingRequest();
        await FlatBufferFrameIO.WriteFrameAsync(_stream!, request, cancellationToken);

        byte[]? response = await FlatBufferFrameIO.ReadFrameAsync(_stream!, cancellationToken);
        if (response is null)
            throw new IOException("Connection closed before FlatBuffer pong response.");

        return FlatBufferControlProtocol.ParsePongResponse(response);
    }

    public async Task<FlatBufferStatusInfo> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        byte[] request = FlatBufferControlProtocol.BuildStatusRequest();
        await FlatBufferFrameIO.WriteFrameAsync(_stream!, request, cancellationToken);

        byte[]? response = await FlatBufferFrameIO.ReadFrameAsync(_stream!, cancellationToken);
        if (response is null)
            throw new IOException("Connection closed before FlatBuffer status response.");

        return FlatBufferControlProtocol.ParseStatusResponse(response);
    }

    public async Task<bool> StartStreamAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        byte[] request = FlatBufferControlProtocol.BuildStartStreamRequest();
        await FlatBufferFrameIO.WriteFrameAsync(_stream!, request, cancellationToken);

        byte[]? response = await FlatBufferFrameIO.ReadFrameAsync(_stream!, cancellationToken);
        if (response is null)
            throw new IOException("Connection closed before FlatBuffer start-stream response.");

        return FlatBufferControlProtocol.ParseStartStreamResponse(response);
    }

    public async Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        byte[] request = FlatBufferControlProtocol.BuildDisconnectRequest();
        await FlatBufferFrameIO.WriteFrameAsync(_stream!, request, cancellationToken);

        byte[]? response = await FlatBufferFrameIO.ReadFrameAsync(_stream!, cancellationToken);
        if (response is null)
            throw new IOException("Connection closed before FlatBuffer disconnect response.");

        return FlatBufferControlProtocol.ParseDisconnectResponse(response);
    }

    public async Task<FlatBufferStreamConfigInfo> ConfigureStreamAsync(
        uint sampleRate = 48000,
        byte channels = 2,
        SampleFormat sampleFormat = SampleFormat.PCM16,
        CodecType codec = CodecType.PCM,
        uint frameSamples = 480,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        byte[] request = FlatBufferControlProtocol.BuildStreamConfigRequest(
            sampleRate: sampleRate,
            channels: channels,
            sampleFormat: sampleFormat,
            codec: codec,
            frameSamples: frameSamples);

        await FlatBufferFrameIO.WriteFrameAsync(_stream!, request, cancellationToken);

        byte[]? response = await FlatBufferFrameIO.ReadFrameAsync(_stream!, cancellationToken);
        if (response is null)
            throw new IOException("Connection closed before FlatBuffer stream-config response.");

        return FlatBufferControlProtocol.ParseStreamConfigResponse(response);
    }

    private void EnsureConnected()
    {
        if (_stream is null || _client is null || !_client.Connected)
            throw new InvalidOperationException("FlatBuffer client is not connected.");
    }

    public ValueTask DisposeAsync()
    {
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
        return ValueTask.CompletedTask;
    }
}