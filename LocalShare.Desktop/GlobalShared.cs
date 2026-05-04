
namespace LocalShare.Desktop
{
    internal static class GlobalShared
    {
        internal static string? IpAddress = "127.0.0.1";
        internal static string? NodeName = "";
        internal static int ServerPort = 11166;
        internal static int BroadcastPort = 9988;
        internal static string? DownloadPath = string.Empty;
        internal static int SendNodeMaxCount = 3;
        internal static int SameNodeMaxSendFileCount = 3;
        internal static string MulticastAddress = "239.255.255.250";
        internal static int TransferSpeed = 256;  //单位 KB

        internal static int MulticastPort = 9989;
    }
}
