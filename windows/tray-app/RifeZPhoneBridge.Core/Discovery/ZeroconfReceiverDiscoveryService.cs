using Zeroconf;

namespace RifeZPhoneBridge.Core.Discovery;

public sealed class ZeroconfReceiverDiscoveryService : IReceiverDiscoveryService
{
    private readonly string _serviceType;
    private readonly TimeSpan _scanTimeout;

    public ZeroconfReceiverDiscoveryService(
        string serviceType = "_rifezaudio._tcp.local.",
        TimeSpan? scanTimeout = null)
    {
        _serviceType = serviceType;
        _scanTimeout = scanTimeout ?? TimeSpan.FromSeconds(5);
    }

    public async Task<IReadOnlyList<NsdReceiverInfo>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IZeroconfHost> hosts = await ZeroconfResolver.ResolveAsync(
            protocol: _serviceType,
            scanTime: _scanTimeout,
            retries: 2,
            cancellationToken: cancellationToken);

        var receivers = new List<NsdReceiverInfo>();

        foreach (var host in hosts)
        {
            if (host.Services is null || host.Services.Count == 0)
                continue;

            if (string.IsNullOrWhiteSpace(host.IPAddress))
                continue;

            foreach (var serviceEntry in host.Services)
            {
                IService service = serviceEntry.Value;

                if (service.Port <= 0)
                    continue;

                string serviceName = ResolveFriendlyServiceName(host, serviceEntry.Key, service);

                receivers.Add(new NsdReceiverInfo(
                    serviceName: serviceName,
                    host: host.IPAddress,
                    port: service.Port));
            }
        }

        return receivers
            .GroupBy(r => $"{r.ServiceName}|{r.Host}|{r.Port}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(r => r.ServiceName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ResolveFriendlyServiceName(IZeroconfHost host, string serviceKey, IService service)
    {
        if (!string.IsNullOrWhiteSpace(service.Name) &&
            !string.Equals(service.Name, "_rifezaudio._tcp.local.", StringComparison.OrdinalIgnoreCase))
        {
            return service.Name;
        }

        if (!string.IsNullOrWhiteSpace(host.DisplayName) &&
            !string.Equals(host.DisplayName, "_rifezaudio._tcp.local.", StringComparison.OrdinalIgnoreCase))
        {
            return host.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(host.Id) &&
            !string.Equals(host.Id, "_rifezaudio._tcp.local.", StringComparison.OrdinalIgnoreCase))
        {
            return host.Id;
        }

        return serviceKey;
    }
}