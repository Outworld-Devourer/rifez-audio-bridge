using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RifeZPhoneBridge.Core.Protocol;

public sealed class FlatBufferStatusInfo
{
    public string State { get; }
    public string DeviceName { get; }
    public string SourceName { get; }

    public FlatBufferStatusInfo(string state, string deviceName, string sourceName)
    {
        State = state;
        DeviceName = deviceName;
        SourceName = sourceName;
    }

    public override string ToString() => $"{State} | {DeviceName} | {SourceName}";
}