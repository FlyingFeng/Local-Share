using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Grpc.Core;
using HandyControl.Controls;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.DataContext.Entities;
using LocalShare.Desktop.FileHandler;
using LocalShare.Protocol.Define;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        [ObservableProperty]
        private int state = 0;  // 0: 在线, 1: 离线

        public ObservableCollection<FileTaskModel> FileTasks { get; set; } = [];

        private Channel? _channel;
        private CancellationTokenSource? _tokenSource;
        private readonly Dictionary<string, Task> sendFileTasks = new Dictionary<string, Task>();


        public bool IsSending => sendFileTasks.Count > 0;

        private async Task CheckAlive()
        {
            while (true)
            {
                try
                {
                    if (_tokenSource == null || _tokenSource.IsCancellationRequested)
                    {
                        break;
                    }

                    LocalShareService.LocalShareServiceClient _client = new LocalShareService.LocalShareServiceClient(_channel);
                    await _client.GetServerNodeInfoAsync(new EmptyMessage(), deadline: DateTime.UtcNow.AddSeconds(10));
                    State = 0;
                    await Task.Delay(1000, _tokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    State = 1;
                    await CloseAsync();
                    Log.Error($"LocalNode.CheckAlive error, nodeName= {NodeName},{ex.Message}\n{ex.StackTrace}");
                    break;
                }

            }
        }


        [RelayCommand]
        private void RemoveFileTask(object args)
        {
            if (args is string fileName)
            {
                var matchedFile = FileTasks.FirstOrDefault(s => s.FileName == fileName);
                if (matchedFile != null)
                {
                    if (matchedFile.State == 0 ||
                        matchedFile.State == 3 ||
                        matchedFile.State == 4)
                    {
                        FileTasks.Remove(matchedFile);
                        WeakReferenceMessenger.Default.Send(new MessageModel
                        {
                            MessageType = MessageType.RemoveCurrentNodeFinishedSendFileTask,
                            Data = fileName
                        });
                    }
                }
            }
        }

        public async Task UpdateNodeStateAsync()
        {
            try
            {
                if (State == 1)
                {
                    _tokenSource = new CancellationTokenSource();
                    if (_channel == null)
                    {
                        _channel = new Channel($"{IpAddress}:{Port}", ChannelCredentials.Insecure);
                    }
                    await _channel.ConnectAsync();
                    if (_channel.State == ChannelState.Ready)
                    {
                        State = 0;
                        _ = CheckAlive();
                        _ = RunLoop();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"LocalNode.UpdateNodeStateAsync error, IpAddress={IpAddress}, Port={Port}\n{ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task InitAsync()
        {
            try
            {
                _tokenSource = new CancellationTokenSource();
                _channel = new Channel($"{IpAddress}:{Port}", ChannelCredentials.Insecure);
                await _channel.ConnectAsync();
                if (_channel.State == ChannelState.Ready)
                {
                    _ = RunLoop();
                    _ = CheckAlive();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"LocalNode.InitAsync error, IpAddress={IpAddress}, Port={Port}\n{ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task FinishSendFile(FileTaskModel model)
        {
            var matched = FileTasks!.FirstOrDefault(s => s.FileName == model.FileName);
            if (matched != null)
            {
                matched.State = 3;
                //FileTasks.Remove(matched);
                using var dbContext = new LocalDataContext();
                var entity = dbContext.SendFileTasks.FirstOrDefault(s => s.TaskId == model.TaskId);
                if (entity != null)
                {
                    entity.State = 3;
                    entity.LastUpdateTime = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();
                }
                sendFileTasks.Remove(model.TaskId);
                Growl.Info($"文件发送完成，文件名：{model.FileName}");
            }
        }


        private async Task RunLoop()
        {
            while (true)
            {
                try
                {
                    if (_tokenSource == null || _tokenSource.IsCancellationRequested)
                    {
                        break;
                    }
                    if (State != 0)
                    {
                        break;
                    }

                    var currentScheduleCount = sendFileTasks.Count;
                    var validCount = GlobalShared.SameNodeMaxSendFileCount - currentScheduleCount;

                    var tmp = FileTasks!.Where(s => s.State == (int)SendFileTaskState.WaitForSchedule).Take(validCount).ToList();
                    if (tmp != null && tmp.Count > 0)
                    {
                        using var dbContext = new LocalDataContext();
                        foreach (var item in tmp)
                        {
                            if (File.Exists(item.FullFileName))
                            {
                                FileInfo fi = new FileInfo(item.FullFileName);
                                var entity = new SendFileTaskEntity
                                {
                                    FileFullName = item.FullFileName,
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
                                item.TaskId = entity.TaskId;
                                SendFileHandler handler = new SendFileHandler(_channel!, IpAddress, NodeName, this);
                                var task = handler.SendFile(item);
                                sendFileTasks[item.TaskId] = task;
                                entity.State = (int)SendFileTaskState.Sending;
                                await dbContext.SaveChangesAsync();
                                item.State = (int)SendFileTaskState.Sending;
                            }
                            await Task.Delay(500, _tokenSource.Token);
                        }
                    }

                    await Task.Delay(1000, _tokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error($"LocalNode.RunLoop error, nodeName= {NodeName},{ex.Message}\n{ex.StackTrace}");
                }
            }
        }



        public async Task CloseAsync()
        {
            try
            {
                Log.Information($"Close LocalNode, NodeName={NodeName}, IpAddress={IpAddress}, Port={Port}");
                _tokenSource?.Cancel();
                if (_channel != null)
                {
                    await _channel.ShutdownAsync();
                }
                _channel = null;
                _tokenSource = null;
                //FileTasks?.Clear();
            }
            catch (Exception ex)
            {
                Log.Error($"LocalNode.CloseAsync error, NodeName={NodeName}, IpAddress={IpAddress}, Port={Port}\n{ex.Message}\n{ex.StackTrace}");
            }
        }

    }
}
