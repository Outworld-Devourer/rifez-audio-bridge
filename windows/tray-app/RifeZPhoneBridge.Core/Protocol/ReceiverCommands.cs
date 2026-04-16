using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RifeZPhoneBridge.Core.Protocol;

public static class ReceiverCommands
{
    public static string Hello(string clientName) => $"HELLO {clientName}";
    public const string Ping = "PING";
    public const string GetStatus = "GET_STATUS";
    public const string StreamStart = "STREAM_START";
    public const string Disconnect = "DISCONNECT";
}