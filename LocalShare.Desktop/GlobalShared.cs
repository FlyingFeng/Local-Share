
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.AspNetCore.Authentication;
using System.Windows.Media;

namespace LocalShare.Desktop
{

    public partial class LocalShareSetting : ObservableObject
    {
        [ObservableProperty]
        private string nodeName = string.Empty;
        [ObservableProperty]
        private string ipAddress = string.Empty;
    }

    internal static class GlobalShared
    {
        internal static string IpAddress = "127.0.0.1";
        internal static string NodeName = "";
        internal static int ServerPort = 11166;
        internal static int BroadcastPort = 9988;
        internal static string SaveFilePath = string.Empty;
        internal static int SendNodeMaxCount = 3;
        internal static int SameNodeMaxSendFileCount = 3;
        internal static string MulticastAddress = "239.239.239.251";
        internal static int TransferSpeed = 256;  //单位 KB
        internal static int DownloadSpeed = 256;   //单位 KB
        internal static bool ShowTrayIcon = true;

        internal static int HttpPort = 0;

        internal static int MulticastPort = 9989;
    }



    internal static class LocalSingleton
    {
        internal static bool HasRegisterMulticast = false;
    }

    internal static class UIShared
    {
        private static SolidColorBrush _red = new SolidColorBrush(Colors.Red);
        private static SolidColorBrush _green = new SolidColorBrush(Colors.Green);
        private static SolidColorBrush _black = new SolidColorBrush(Colors.Black);
        private static SolidColorBrush _yellow = new SolidColorBrush(Colors.Yellow);
        private static SolidColorBrush _blue = new SolidColorBrush(Colors.Blue);

        public static SolidColorBrush Black => _black;
        public static SolidColorBrush Red => _red;
        public static SolidColorBrush Green => _green;
        public static SolidColorBrush Yellow => _yellow;
        public static SolidColorBrush Blue => _blue;
    }

}
