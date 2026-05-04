using Google.Protobuf;
using LocalShare.Protocol.Define;
using Serilog;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace LocalShare.Desktop
{
    public class UdpBrocastDiscoveryService : IDisposable
    {
        private const int BroadcastInterval = 3000; // 3秒

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
        /// 启动广播和监听
        /// </summary>
        public void Start()
        {
            Dispose();
            _listener = new UdpClient();
            // 允许多个程序绑定同一端口（同机多实例时不报错）
            _listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Client.Bind(new IPEndPoint(IPAddress.Any, GlobalShared.BroadcastPort));
            _cts = new CancellationTokenSource();

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
                    Log.Warning($"退出广播");
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
                    var result = await _listener!.ReceiveAsync(ct);
                    HandleMessage(result);
                }
                catch (OperationCanceledException)
                {
                    Log.Warning($"退出广播监听");
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
            try
            {
                _cts?.Cancel();
                _listener?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"UdpBrocastDiscoveryService.Dispose error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _cts = null;
                _listener = null;
            }
        }
    }
}
