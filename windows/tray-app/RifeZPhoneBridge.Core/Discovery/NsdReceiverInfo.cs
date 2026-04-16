using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RifeZPhoneBridge.Core.Discovery;

public sealed class NsdReceiverInfo
{
    public string ServiceName { get; }
    public string Host { get; }
    public int Port { get; }

    public NsdReceiverInfo(string serviceName, string host, int port)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name must not be empty.", nameof(serviceName));

        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host must not be empty.", nameof(host));

        if (port <= 0 || port > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");

        ServiceName = serviceName;
        Host = host;
        Port = port;
    }

    public override string ToString() => $"{ServiceName} ({Host}:{Port})";
}