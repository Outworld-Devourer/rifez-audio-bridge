using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RifeZPhoneBridge.Core.Protocol
{
    public static class ReceiverResponses
    {
        public const string OkPrefix = "OK ";
        public const string ErrorPrefix = "ERR ";

        public static bool IsOk(string? line) =>
            !string.IsNullOrWhiteSpace(line) &&
            line.StartsWith(OkPrefix, StringComparison.OrdinalIgnoreCase);

        public static bool IsError(string? line) =>
            !string.IsNullOrWhiteSpace(line) &&
            line.StartsWith(ErrorPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
