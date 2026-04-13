using Google.Protobuf;
using LocalShare.Protocol.Define;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LocalShare.Desktop
{
    public class UdpDiscoveryService : IDisposable
    {
        //private const int BroadcastPort = 9988;
        private const int BroadcastInterval = 3000; // 3秒

        //private readonly string _localIP;
        private readonly UdpClient _listener;
        private readonly CancellationTokenSource _cts = new();

        private ConcurrentDictionary<string, NodeModel> nodeCaches = new();

        // 发现新客户端时触发
        public event Action<NodeModel>? ClientDiscovered;

        public UdpDiscoveryService()
        {
            //_localIP = GlobalShared.IpAddress ?? "127.0.0.1";

            _listener = new UdpClient();
            // 允许多个程序绑定同一端口（同机多实例时不报错）
            _listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Client.Bind(new IPEndPoint(IPAddress.Any, GlobalShared.BroadcastPort));
        }

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
        /// 启动广播和监听
        /// </summary>
        public void Start()
        {
            Task.Run(() => BroadcastLoopAsync(_cts.Token));
            Task.Run(() => ListenLoopAsync(_cts.Token));
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        //public void Stop() => _cts.Cancel();

        // ── 广播循环 ────────────────────────────────────────────────

        private async Task BroadcastLoopAsync(CancellationToken ct)
        {
            using var sender = new UdpClient();
            sender.EnableBroadcast = true;

            var broadcast = new IPEndPoint(IPAddress.Broadcast, GlobalShared.BroadcastPort);

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
                        await sender.SendAsync(data, data.Length, broadcast);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Warning($"[广播异常] {ex.Message}");
                    await Task.Delay(1000, ct); // 出错后等 1 秒重试
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
                    var result = await _listener.ReceiveAsync(ct);
                    HandleMessage(result);
                }
                catch (OperationCanceledException)
                {
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

        // ── 工具方法 ─────────────────────────────────────────────────
        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _listener.Dispose();
        }
    }
}
