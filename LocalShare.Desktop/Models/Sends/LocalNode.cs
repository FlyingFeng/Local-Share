using CommunityToolkit.Mvvm.ComponentModel;
using Grpc.Core;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.DataContext.Entities;
using LocalShare.Desktop.FileHandler;
using LocalShare.Protocol.Define;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.Models.Sends
{
    public partial class LocalNode : ObservableObject
    {
        [ObservableProperty]
        private string nodeName = string.Empty;
        [ObservableProperty]
        private bool inWhiteList;
        [ObservableProperty]
        private bool inBlackList;
        [ObservableProperty]
        private bool isSelected;
        [ObservableProperty]
        private long lastSeenTime;
        [ObservableProperty]
        private string ipAddress = string.Empty;
        [ObservableProperty]
        private int port;

        private readonly IServiceProvider _serviceProvider;

        public LocalNode(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ObservableCollection<FileTaskModel>? FileTasks { get; set; }

        private Channel? _channel;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public async Task InitAsync()
        {
            try
            {
                _channel = new Channel($"{IpAddress}:{Port}", ChannelCredentials.Insecure);
                await _channel.ConnectAsync();
                if (_channel.State == ChannelState.Ready)
                {
                    _ = RunLoop();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"LocalNode.InitAsync error, IpAddress={IpAddress}, Port={Port}\n{ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }


        private async Task RunLoop()
        {
            while (true)
            {
                var tmp = FileTasks!.Where(s => s.State == (int)SendFileTaskState.WaitForSchedule).ToList();
                if (tmp != null && tmp.Count > 0)
                {
                    var dbContext = _serviceProvider.GetService<LocalDataContext>();
                    foreach (var item in tmp)
                    {
                        if (File.Exists(item.FileName))
                        {
                            FileInfo fi = new FileInfo(item.FileName);
                            var entity = new SendFileTaskEntity
                            {
                                FileFullName = item.FileName,
                                FileName = fi.Name,
                                InitTime = DateTime.UtcNow,
                                LastUpdateTime = DateTime.UtcNow,
                                ReceiveIpAddress = IpAddress,
                                SendIpAddress = GlobalShared.IpAddress!,
                                ReceiveNodeName = NodeName,
                                SendNodeName = GlobalShared.NodeName!,
                                State = (int)SendFileTaskState.WaitForSchedule,
                                TaskId = Guid.NewGuid().ToString()
                            };
                            await dbContext!.AddAsync(entity);
                            await dbContext!.SaveChangesAsync();

                            SendFileHandler handler = new SendFileHandler(_channel!, IpAddress, NodeName, _serviceProvider);
                            var task = handler.SendFile(new SendFileModel
                            {
                                FileName = fi.Name,
                                FilePath = fi.FullName,
                                IsOpenFromDir = item.IsOpenFromDir,
                                MD5 = item.Md5,
                                TaskId = entity.TaskId,
                            });
                            entity.State = (int)SendFileTaskState.Sending;
                            await dbContext.SaveChangesAsync();
                            item.State = (int)SendFileTaskState.Sending;
                        }
                    }
                }

                await Task.Delay(1000);
            }
        }



        public async Task CloseAsync()
        {
            try
            {
                _tokenSource.Cancel();
                await _channel!.ShutdownAsync();
                _channel = null;
                FileTasks?.Clear();
            }
            catch (Exception ex)
            {
                Log.Error($"LocalNode.CloseAsync error, NodeName={NodeName}, IpAddress={IpAddress}, Port={Port}\n{ex.Message}\n{ex.StackTrace}");
            }
        }

    }
}
