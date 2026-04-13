using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop
{
    internal static class GlobalShared
    {
        internal static string? IpAddress = "127.0.0.1";
        internal static string? NodeName = "";
        internal static int ServerPort = 11167;
        internal static int BroadcastPort = 9988;
        internal static string? DownloadPath = string.Empty;
        internal static int SendNodeMaxCount;
        internal static int SameNodeMaxSendFileCount;
    }
}
