using Google.Protobuf;
using LocalShare.Protocol.Define;
using Serilog;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace LocalShare.Desktop
{
    public class UdpMulticastDiscoveryService : IDisposable
    {
        private const int BroadcastInterval = 3000; // 3秒
        //private const string MulticastGroup = "239.255.255.250"; // 组播地址
        private const int MulticastTtl = 32; // 组播 TTL

        private UdpClient? _listener;
        private CancellationTokenSource? _cts;

        private ConcurrentDictionary<string, NodeModel> nodeCaches = new();

        // 发现新客户端时触发
        public event Action<NodeModel>? ClientDiscovered;

        public List<NodeModel> GetNodeModelCaches()
        {
            var utcNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var list = nodeCaches.ToArray()
                                 .Select(s => s.Value)
                                 .Where(s => Math.Abs(utcNow - s.Time) <= 5)
                                 .ToList();
            return list;
        }

        /// <summary>
        /// 启动组播发送和监听
        /// </summary>
        public void Start()
        {
            Dispose();

            _listener = new UdpClient();
            _listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Client.Bind(new IPEndPoint(IPAddress.Any, GlobalShared.BroadcastPort));

            // 加入组播组
            _listener.JoinMulticastGroup(IPAddress.Parse(GlobalShared.MulticastAddress));

            _cts = new CancellationTokenSource();
            Task.Run(() => MulticastSendLoopAsync(_cts.Token));
            Task.Run(() => ListenLoopAsync(_cts.Token));
        }

        // ── 组播发送循环 ─────────────────────────────────────────────

        private async Task MulticastSendLoopAsync(CancellationToken ct)
        {
            using var sender = new UdpClient();
            sender.MulticastLoopback = true; // 本机也能收到（同机多实例时有用，可按需改为 false）
            sender.Client.SetSocketOption(
                SocketOptionLevel.IP,
                SocketOptionName.MulticastTimeToLive,
                MulticastTtl);

            var multicastEndPoint = new IPEndPoint(IPAddress.Parse(GlobalShared.MulticastAddress), GlobalShared.BroadcastPort);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(BroadcastInterval, ct);

                    if (!string.IsNullOrEmpty(GlobalShared.IpAddress) &&
                        !string.IsNullOrEmpty(GlobalShared.NodeName))
                    {
                        var nodeModel = new NodeModel
                        {
                            NodeName = GlobalShared.NodeName,
                            IpAddress = GlobalShared.IpAddress,
                            MachineName = Environment.MachineName,
                            Port = GlobalShared.ServerPort,
                            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };
                        var data = nodeModel.ToByteArray();
                        await sender.SendAsync(data, data.Length, multicastEndPoint);
                    }
                }
                catch (OperationCanceledException)
                {
                    Log.Warning("退出组播发送");
                    break;
                }
                catch (Exception ex)
                {
                    Log.Warning($"[组播发送异常] {ex.Message}");
                    await Task.Delay(1000, ct);
                }
            }
        }

        // ── 监听循环 ────────────────────────────────────────────────

        private async Task ListenLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = await _listener!.ReceiveAsync(ct);
                    HandleMessage(result);
                }
                catch (OperationCanceledException)
                {
                    Log.Warning("退出组播监听");
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error($"[监听异常] {ex.Message}");
                }
            }
        }

        // ── 处理收到的消息 ───────────────────────────────────────────

        private void HandleMessage(UdpReceiveResult result)
        {
            var data = NodeModel.Parser.ParseFrom(result.Buffer);
            if (data != null &&
                (data.IpAddress != GlobalShared.IpAddress ||
                 data.Port != GlobalShared.ServerPort ||
                 data.NodeName != GlobalShared.NodeName))
            {
                nodeCaches.AddOrUpdate($"{data.NodeName}-{data.IpAddress}-{data.Port}", data, (k, v) => data);
                ClientDiscovered?.Invoke(data);
            }
        }

        // ── 释放资源 ─────────────────────────────────────────────────

        public void Dispose()
        {
            try
            {
                _cts?.Cancel();

                // 离开组播组后再释放，避免残留组播订阅
                try { _listener?.DropMulticastGroup(IPAddress.Parse(GlobalShared.MulticastAddress)); }
                catch { /* 忽略离组失败 */ }

                _listener?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"UdpDiscoveryService.Dispose error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _cts = null;
                _listener = null;
            }
        }
    }
}
