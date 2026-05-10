using Grpc.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.KeepStates
{
    public class RpcChannelHolder
    {
        private Dictionary<string, Channel> _channel = new Dictionary<string, Channel>();
        private readonly object locker = new object();


        public async Task CloseChannel(string targetIp, int targetPort)
        {
            Channel? channelToClose = null;
            lock (locker)
            {
                var key = $"{targetIp}:{targetPort}";
                if (_channel.TryGetValue(key, out var channel))
                {
                    channelToClose = channel;
                    _channel.Remove(key);
                }
            }

            try
            {
                if (channelToClose != null)
                {
                    await channelToClose.ShutdownAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"CloseChannel error, {ex.Message}\n{ex.StackTrace}");
            }
        }


        public Channel? GetChannel(string targetIp, int targetPort)
        {
            lock (locker)
            {
                var key = $"{targetIp}:{targetPort}";
                if (!_channel.TryGetValue(key, out var channel))
                {
                    channel = new Channel(key, ChannelCredentials.Insecure);
                    _channel.Add(key, channel);
                }
                return channel;
            }
        }

        public async Task<Channel> AddAndCloseOldChannel(string targetIp, int targetPort)
        {
            Channel? oldChannel = null;
            Channel? newChannel = null;
            lock (locker)
            {
                if (!string.IsNullOrEmpty(targetIp) && targetPort > 1024)
                {
                    var key = $"{targetIp}:{targetPort}";
                    if (_channel.TryGetValue(key, out var val))
                    {
                        oldChannel = val;
                        _channel.Remove(key);
                    }
                    var channel = new Channel(key, ChannelCredentials.Insecure);
                    _channel.Add(key, channel);
                    newChannel = channel;
                }
            }

            try
            {
                if (oldChannel != null)
                {
                    await oldChannel.ShutdownAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"AddChennel close old channel error, {ex.Message}\n{ex.StackTrace}");
            }
            return newChannel;
        }

    }
}
