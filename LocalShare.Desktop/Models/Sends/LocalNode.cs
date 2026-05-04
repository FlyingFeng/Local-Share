using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Grpc.Core;
using HandyControl.Controls;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.DataContext.Entities;
using LocalShare.Desktop.FileHandler;
using LocalShare.Protocol.Define;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

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
        [ObservableProperty]
        private bool showBadge = false;

        public ObservableCollection<FileTaskModel> FileTasks { get; set; } = [];

        private Channel? _channel;
        private CancellationTokenSource? _tokenSource;
        private readonly Dictionary<string, SendFileHandler> sendFileTasks = new Dictionary<string, SendFileHandler>();

        public bool IsSending => sendFileTasks.Count > 0;

        public void ReceiveChatMessage(ChatRequest request)
        {
            ShowBadge = true;
            var messageInfo = new MessageInfo
            {
                ChatId = request.ChatId,
                Message = request.Message,
                Time = request.Time,
                Type = 1
            };
            ChatMessageHolder.AddChatHistoryData(request.SendNodeName, messageInfo);
            WeakReferenceMessenger.Default.Send(new MessageModel
            {
                MessageType = MessageType.ChatMessageArrived,
                Data = messageInfo
            });
        }

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
                    await Task.Delay(3000, _tokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    State = 1;
                    Close();
                    Log.Error($"LocalNode.CheckAlive error, nodeName= {NodeName},{ex.Message}\n{ex.StackTrace}");
                    break;
                }

            }
        }

        [RelayCommand]
        private void ShowChatWindow()
        {
            try
            {
                ChatWindow window = new ChatWindow();
                window.NodeChannel = _channel;
                window.Node = this;
                window.Title = NodeName;
                window.Owner = Application.Current.MainWindow;
                ShowBadge = false;
                window.ShowDialog();
            }
            catch
            {
            }
        }

        [RelayCommand]
        private async Task CancelFileTask(object args)
        {
            if (args is string fileName)
            {
                try
                {
                    var matchedFile = FileTasks.FirstOrDefault(s => s.FileName == fileName);
                    if (matchedFile != null)
                    {
                        if (sendFileTasks.TryGetValue(matchedFile.TaskId, out var handler))
                        {
                            handler.CancelFileTask();
                            sendFileTasks.Remove(matchedFile.TaskId);
                        }

                        LocalShareService.LocalShareServiceClient client = new LocalShareService.LocalShareServiceClient(_channel);
                        try
                        {
                            await client.OperateFileTaskAsync(new FileOperationRequest
                            {
                                FileName = fileName,
                                OperationType = 2,
                                SendNodeName = GlobalShared.NodeName,
                                TaskId = matchedFile.TaskId,
                                Sender = 0
                            });

                        }
                        catch (Exception ex2)
                        {
                            Log.Error($"CancelFileTask.inner error, {ex2.Message}\n{ex2.StackTrace}");
                        }
                    }
                    await Task.Delay(100);
                    if (matchedFile != null)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            FileTasks.Remove(matchedFile);
                        });
                    }
                    WeakReferenceMessenger.Default.Send(new MessageModel
                    {
                        MessageType = MessageType.RemoveCurrentNodeFinishedSendFileTask,
                        Data = fileName
                    });
                }
                catch (Exception ex)
                {
                    HandyControl.Controls.MessageBox.Show($"取消发送失败，文件名：{args}\n错误信息：{ex.Message}");
                }
            }
        }


        public async Task UpdateNodeStateAsync()
        {
            try
            {
                if (State == 1)
                {
                    State = 0;
                    _tokenSource = new CancellationTokenSource();
                    if (_channel == null)
                    {
                        _channel = new Channel($"{IpAddress}:{Port}", ChannelCredentials.Insecure);
                    }
                    await _channel.ConnectAsync(DateTime.UtcNow.AddSeconds(5));
                    if (_channel.State == ChannelState.Ready)
                    {
                        State = 0;
                        _ = CheckAlive();
                        //_ = RunLoop();
                    }
                }
            }
            catch (Exception ex)
            {
                State = 1;
                _tokenSource?.Cancel();
                Log.Error($"LocalNode.UpdateNodeStateAsync error, IpAddress={IpAddress}, Port={Port}\n{ex.Message}\n{ex.StackTrace}");
                throw;
            }
            finally
            {

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
            try
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
            catch (Exception ex)
            {
                Log.Error($"FinishSendFile error, {ex.Message}\n{ex.StackTrace}");
            }
        }


        private async Task RunLoop()
        {
            while (true)
            {
                try
                {
                    if (State != 0)
                    {
                        await Task.Delay(1000);
                        continue;
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
                                    State = (int)SendFileTaskState.Transferring,
                                    TaskId = item.TaskId
                                };
                                await dbContext!.AddAsync(entity);
                                await dbContext!.SaveChangesAsync();
                                SendFileHandler handler = new SendFileHandler(_channel!, NodeName, this);
                                _ = handler.SendFile(item, entity);
                                item.State = (int)SendFileTaskState.Transferring;
                                sendFileTasks[item.TaskId] = handler;
                            }
                            await Task.Delay(500);
                        }
                    }

                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    Log.Error($"LocalNode.RunLoop error, nodeName= {NodeName},{ex.Message}\n{ex.StackTrace}");
                }
            }
        }



        public void Close()
        {
            try
            {
                Log.Information($"Close LocalNode, NodeName={NodeName}, IpAddress={IpAddress}, Port={Port}");
                _tokenSource?.Cancel();
                //if (_channel != null)
                //{
                //    await _channel.ShutdownAsync();
                //}
                //_channel = null;
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
