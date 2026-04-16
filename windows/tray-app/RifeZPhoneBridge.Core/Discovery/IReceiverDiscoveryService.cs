using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RifeZPhoneBridge.Core.Discovery;

public interface IReceiverDiscoveryService
{
    Task<IReadOnlyList<NsdReceiverInfo>> DiscoverAsync(CancellationToken cancellationToken = default);
}
