using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RifeZPhoneBridge.Core.Models;

public sealed class PhoneEndpoint
{
    public string Host { get; }
    public int Port { get; }
    public string? DisplayName { get; }

    public PhoneEndpoint(string host, int port, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host must not be empty.", nameof(host));

        if (port <= 0 || port > 65535)
            throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");

        Host = host;
        Port = port;
        DisplayName = displayName;
    }

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(DisplayName)
            ? $"{Host}:{Port}"
            : $"{DisplayName} ({Host}:{Port})";
    }
}
