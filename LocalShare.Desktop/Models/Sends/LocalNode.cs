using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Grpc.Core;
using HandyControl.Controls;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.DataContext.Entities;
using LocalShare.Desktop.FileHandler;
using LocalShare.Desktop.KeepStates;
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

        private int checkDuration = 3000;
        public ObservableCollection<FileTaskModel> FileTasks { get; set; } = [];

        private readonly SemaphoreSlim _initSlim = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, SendFileHandler> sendFileTasks = new Dictionary<string, SendFileHandler>();
        private readonly List<FileItemInfo> _cacheShareFiles = new List<FileItemInfo>();

        private readonly object locker = new object();
        public bool IsSending => sendFileTasks.Count > 0;

        private readonly RpcChannelHolder? _rpcChannelHolder;
        public LocalNode(RpcChannelHolder? rpcChannelHolder)
        {
            _rpcChannelHolder = rpcChannelHolder;
        }

        public Channel? GetRpcChannel()
        {
            var channel = _rpcChannelHolder!.GetChannel(IpAddress, Port);
            return channel;
        }



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

        private FileItemInfo? GetCache(string name)
        {
            lock (locker)
            {
                return _cacheShareFiles.FirstOrDefault(s => s.FileName == name);
            }
        }


        private List<FileItemInfo> GetCaches()
        {
            lock (locker)
            {
                var list = new List<FileItemInfo>();
                list.AddRange(_cacheShareFiles);
                return list;
            }
        }

        private int GetCacheFileCount()
        {
            lock (locker)
            {
                return _cacheShareFiles.Count;
            }
        }

        private void AddCacheFiles(FileItemInfo[] caches)
        {
            lock (locker)
            {
                if (caches != null && caches.Length > 0)
                {
                    _cacheShareFiles.Clear();
                    foreach (var item in caches)
                    {
                        _cacheShareFiles.Add(item);
                    }
                }
            }
        }


        private async Task CheckAlive()
        {
            Log.Information($"Start CheckAlive()");
            while (true)
            {
                try
                {
                    var channel = _rpcChannelHolder!.GetChannel(IpAddress, Port);
                    var _client = new LocalShareService.LocalShareServiceClient(channel);
                    await _client.GetServerNodeInfoAsync(new EmptyMessage(), deadline: DateTime.UtcNow.AddSeconds(3));
                    State = 0;
                    checkDuration = 3000;
                }
                catch (Exception ex)
                {
                    State = 1;
                    checkDuration += 1000;
                    if (checkDuration >= 60 * 1000)
                    {
                        checkDuration = 3000;
                    }
                    Log.Error($"LocalNode.CheckAlive error, nodeName= {NodeName},{ex.Message}\n{ex.StackTrace}");
                }
                finally
                {
                    await Task.Delay(checkDuration);
                }

            }
        }

        [RelayCommand]
        private async Task CheckConnection()
        {
            try
            {
                var channel = _rpcChannelHolder!.GetChannel(IpAddress, Port);
                var _client = new LocalShareService.LocalShareServiceClient(channel);
                await _client.GetServerNodeInfoAsync(new EmptyMessage(), deadline: DateTime.UtcNow.AddSeconds(3));
                State = 0;
                checkDuration = 3000;
                HandyControl.Controls.MessageBox.Show($"节点【{NodeName}】已经在线", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                State = 1;
                checkDuration += 1000;
                if (checkDuration >= 60 * 1000)
                {
                    checkDuration = 3000;
                }
                Log.Error($"CheckConnection error, {ex.Message}\n{ex.StackTrace}");
                HandyControl.Controls.MessageBox.Show($"节点【{NodeName}】不在线", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void ShowChatWindow()
        {
            try
            {
                if (State == 1)
                {
                    HandyControl.Controls.MessageBox.Show("节点已经下线", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ChatWindow window = new ChatWindow();
                window.NodeChannel = _rpcChannelHolder!.GetChannel(IpAddress, Port);
                window.Node = this;
                window.Title = NodeName;
                window.Owner = Application.Current.MainWindow;
                ShowBadge = false;
                window.ShowDialog();
            }
            catch (Exception e)
            {
                Log.Error($"ShowChatWindow error, {e.Message}\n{e.StackTrace}");
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

                        //var channel = _rpcChannelHolder!.GetChannel(IpAddress, Port);
                        //var client = new LocalShareService.LocalShareServiceClient(channel);
                        //try
                        //{
                        //    await client.OperateFileTaskAsync(new FileOperationRequest
                        //    {
                        //        FileName = fileName,
                        //        OperationType = 2,
                        //        SendNodeName = GlobalShared.NodeName,
                        //        TaskId = matchedFile.TaskId,
                        //        Sender = 0
                        //    });
                        //}
                        //catch (Exception ex2)
                        //{
                        //    Log.Error($"CancelFileTask.inner error, {ex2.Message}\n{ex2.StackTrace}");
                        //}
                    }
                    //await Task.Delay(100);
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
                    Log.Error($"CancelFileTask.outter error, {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        public async Task InitAsync()
        {
            try
            {
                await _initSlim.WaitAsync();
                var channel = _rpcChannelHolder!.GetChannel(IpAddress, Port);
                if (channel != null)
                {
                    await channel.ConnectAsync(DateTime.UtcNow.AddSeconds(5));
                    await RefreshCacheFiles();
                    _ = CheckAlive();
                    _ = RunLoop();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"LocalNode.InitAsync error, IpAddress={IpAddress}, Port={Port}\n{ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _initSlim.Release();
            }
        }

        public void FinishSendFile(FileTaskModel model)
        {
            try
            {
                //var matched = FileTasks!.FirstOrDefault(s => s.FileName == model.FileName);
                //if (matched != null)
                //{
                //    matched.State = 3;
                //FileTasks.Remove(matched);
                //using var dbContext = new LocalDataContext();
                //var entity = dbContext.SendFileTasks.FirstOrDefault(s => s.TaskId == model.TaskId);
                //if (entity != null)
                //{
                //    entity.State = 3;
                //    entity.LastUpdateTime = DateTime.UtcNow;
                //    await dbContext.SaveChangesAsync();
                //}
                //}
                sendFileTasks.Remove(model.TaskId);
                //Growl.Info($"文件发送完成，文件名：{model.FileName}");
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

                    await RefreshCacheFiles();

                    var currentScheduleCount = sendFileTasks.Count;
                    var validCount = GlobalShared.SameNodeMaxSendFileCount - currentScheduleCount;

                    var tmp = FileTasks!.Where(s => s.State == (int)SendFileTaskState.WaitForSchedule).Take(validCount).ToList();
                    if (tmp != null && tmp.Count > 0)
                    {
                        var channel = _rpcChannelHolder!.GetChannel(IpAddress, Port);
                        var client = new LocalShareService.LocalShareServiceClient(channel);
                        await client.GetServerNodeInfoAsync(new EmptyMessage(), deadline: DateTime.UtcNow.AddSeconds(5));
                        //using var dbContext = new LocalDataContext();
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
                                //await dbContext!.AddAsync(entity);
                                //await dbContext!.SaveChangesAsync();
                                SendFileHandler handler = new SendFileHandler(channel!, NodeName, this);
                                _ = handler.SendFile(item, entity);
                                item.State = (int)SendFileTaskState.Transferring;
                                sendFileTasks[item.TaskId] = handler;
                            }
                            await Task.Delay(500);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"LocalNode.RunLoop error, nodeName= {NodeName},{ex.Message}\n{ex.StackTrace}");
                }
                finally
                {
                    await Task.Delay(1000);
                }
            }
        }


        public async Task<FileItemInfo?> GetCacheFile(string fileName)
        {
            FileItemInfo? file = null;
            try
            {
                if (GetCacheFileCount() == 0)
                {
                    await RefreshCacheFiles();
                }
                file = GetCache(fileName);
            }
            catch (Exception ex)
            {
                Log.Error($"Node= {NodeName}, GetCacheFiles error, {ex.Message}\n{ex.StackTrace}");
            }
            return file;
        }

        public async Task<List<FileItemInfo>> GetCacheFiles()
        {
            var list = new List<FileItemInfo>();
            try
            {
                if (GetCacheFileCount() == 0)
                {
                    await RefreshCacheFiles();
                }
                list.AddRange(GetCaches());
            }
            catch (Exception ex)
            {
                Log.Error($"Node= {NodeName}, GetCacheFiles error, {ex.Message}\n{ex.StackTrace}");
            }
            return list;
        }

        public async Task RefreshCacheFiles()
        {
            try
            {
                var channel = _rpcChannelHolder!.GetChannel(IpAddress, Port);
                var client = new LocalShareService.LocalShareServiceClient(channel);
                var response = await client.ListFilesAsync(new EmptyMessage());
                if (response.Files.Count > 0)
                {
                    AddCacheFiles(response.Files.ToArray());
                    //lock (locker)
                    //{
                    //    _cacheShareFiles.Clear();
                    //    foreach (var eachFile in response.Files)
                    //    {
                    //        _cacheShareFiles.Add(eachFile);
                    //    }
                    //}
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Node= {NodeName}, RefreshCacheFiles error, {ex.Message}\n{ex.StackTrace}");
            }
        }

    }
}
