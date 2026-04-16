using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RifeZPhoneBridge.Core.Discovery;

public sealed class ManualReceiverDiscoveryService : IReceiverDiscoveryService
{
    private readonly IReadOnlyList<NsdReceiverInfo> _receivers;

    public ManualReceiverDiscoveryService(IEnumerable<NsdReceiverInfo> receivers)
    {
        _receivers = receivers.ToList();
    }

    public Task<IReadOnlyList<NsdReceiverInfo>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_receivers);
    }
}
