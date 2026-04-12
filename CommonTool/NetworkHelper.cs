using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CommonTool
{
    public static class NetworkHelper
    {
        /// <summary>
        /// 获取本机所有 IPv4 地址
        /// </summary>
        public static List<string> GetAllLocalIPv4()
        {
            var ips = new List<string>();

            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                // 只取运行中的接口，排除回环和隧道
                if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    continue;

                var ipProperties = networkInterface.GetIPProperties();

                foreach (var addr in ipProperties.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork) // IPv4
                    {
                        ips.Add(addr.Address.ToString());
                    }
                }
            }

            return ips;
        }

        /// <summary>
        /// 获取本机所有 IPv6 地址
        /// </summary>
        public static List<string> GetAllLocalIPv6()
        {
            var ips = new List<string>();

            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var ipProperties = networkInterface.GetIPProperties();

                foreach (var addr in ipProperties.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        ips.Add(addr.Address.ToString());
                    }
                }
            }

            return ips;
        }

        /// <summary>
        /// 获取指定网卡的 IP（按网卡名称过滤）
        /// </summary>
        public static string? GetIPByInterfaceName(string interfaceName)
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.Name.Contains(interfaceName, StringComparison.OrdinalIgnoreCase)
                         && n.OperationalStatus == OperationalStatus.Up)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                ?.Address.ToString();
        }

        /// <summary>
        /// 通过 UDP 连接获取出口 IP（不会真正发包，最准确）
        /// </summary>
        public static string? GetLocalIPByUdp()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                // 连接任意外部地址（不会真正发包）
                socket.Connect("8.8.8.8", 80);
                return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 通过主机名解析获取 IP
        /// </summary>
        public static List<string> GetIPByHostName()
        {
            var hostName = Dns.GetHostName();
            var hostEntry = Dns.GetHostEntry(hostName);

            return hostEntry.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => ip.ToString())
                .ToList();
        }
    }
}
