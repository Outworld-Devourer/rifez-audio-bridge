using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using RifeZPhoneBridge.Core.Protocol;

namespace RifeZPhoneBridge.Core.Networking
{
    public sealed class TcpPhoneClient : IAsyncDisposable
    {
        private TcpClient? _client;
        private StreamReader? _reader;
        private StreamWriter? _writer;

        public bool IsConnected => _client?.Connected == true;

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            if (IsConnected)
            {
                return;
            }

            _client = new TcpClient();

            using var ctr = cancellationToken.Register(() =>
            {
                try
                {
                    _client?.Dispose();
                }
                catch
                {
                    // Ignore cancellation disposal race.
                }
            });

            await _client.ConnectAsync(host, port);

            NetworkStream stream = _client.GetStream();

            _reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            _writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true)
            {
                AutoFlush = true
            };
        }

        public async Task<string> SendCommandAsync(string command, CancellationToken cancellationToken = default)
        {
            EnsureConnected();

            await _writer!.WriteLineAsync(command);
            string? response = await _reader!.ReadLineAsync(cancellationToken);

            if (response is null)
            {
                throw new IOException("Receiver closed the connection before sending a response.");
            }

            return response;
        }

        public async Task<string> HelloAsync(string clientName, CancellationToken cancellationToken = default)
        {
            return await SendCommandAsync(ReceiverCommands.Hello(clientName), cancellationToken);
        }

        public async Task<string> StreamStartAsync(CancellationToken cancellationToken = default)
        {
            return await SendCommandAsync(ReceiverCommands.StreamStart, cancellationToken);
        }

        public async Task<string> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            return await SendCommandAsync(ReceiverCommands.Disconnect, cancellationToken);
        }
        public async Task<string> PingAsync(CancellationToken cancellationToken = default)
        {
            return await SendCommandAsync(ReceiverCommands.Ping, cancellationToken);
        }

        public async Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            return await SendCommandAsync(ReceiverCommands.GetStatus, cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_writer is not null)
                {
                    await _writer.FlushAsync();
                }
            }
            catch
            {
                // Ignore flush errors on shutdown.
            }

            _reader?.Dispose();
            _writer?.Dispose();
            _client?.Dispose();

            _reader = null;
            _writer = null;
            _client = null;
        }

        private void EnsureConnected()
        {
            if (_client is null || _reader is null || _writer is null || !_client.Connected)
            {
                throw new InvalidOperationException("Phone client is not connected.");
            }
        }
    }
}
