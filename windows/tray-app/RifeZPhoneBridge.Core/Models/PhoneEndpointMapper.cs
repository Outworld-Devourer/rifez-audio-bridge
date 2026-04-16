using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RifeZPhoneBridge.Core.Discovery;

namespace RifeZPhoneBridge.Core.Models;

public static class PhoneEndpointMapper
{
    public static PhoneEndpoint FromNsdReceiver(NsdReceiverInfo receiver)
    {
        return new PhoneEndpoint(
            host: receiver.Host,
            port: receiver.Port,
            displayName: receiver.ServiceName
        );
    }
}
