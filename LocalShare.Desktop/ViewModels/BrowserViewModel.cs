using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HandyControl.Controls;
using LocalShare.Desktop.FileHandler;
using LocalShare.Desktop.KeepStates;
using LocalShare.Desktop.Models;
using LocalShare.Desktop.Models.Browsers;
using LocalShare.Desktop.Models.Sends;
using LocalShare.Protocol.Define;
using Serilog;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace LocalShare.Desktop.ViewModels
{
    public partial class BrowserViewModel : ObservableObject, IClosable, IRecipient<MessageModel>
    {
        public ObservableCollection<RemoteNodeItem> FileNodes { get; set; } = [];

        private readonly Dictionary<string, DownloadFileHandler> _downloadTask = new Dictionary<string, DownloadFileHandler>();

        [ObservableProperty]
        private int nowIndex = 0;

        [ObservableProperty]
        private bool refreshEnabled = true;


        public BrowserViewModel() { }

        public BrowserViewModel(SendDataHolder? sendDataHolder, DownloadDataHolder? downloadDataHolder)
        {
            SendDataHolder = sendDataHolder;
            DownloadDataHolder = downloadDataHolder;
            WeakReferenceMessenger.Default.Register<MessageModel>(this);
        }
        public DownloadDataHolder? DownloadDataHolder { get; set; }
        public SendDataHolder? SendDataHolder { get; set; }

        public void Close()
        {
            WeakReferenceMessenger.Default.Unregister<MessageModel>(this);
        }

        [RelayCommand]
        private async Task RefreshFile()
        {
            try
            {
                RefreshEnabled = false;
                if (SendDataHolder!.Nodes.Count > 0 && NowIndex >= 0)
                {
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RefreshFile error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                RefreshEnabled = true;
            }
        }

        [RelayCommand]
        private void CancelDownloadFileTask(object args)
        {
            if (args != null && args is DownloadFileTaskItem model)
            {
                var key = $"{model.NodeName}#{model.FileName}";
                if (_downloadTask.TryGetValue(key, out var handler))
                {
                    handler.Cancel();
                    _downloadTask.Remove(key);
                }
                var matched = DownloadDataHolder!.Get(model.FileName);
                if (matched != null)
                {
                    DownloadDataHolder!.Remove(matched.Id);
                }
            }
        }

        [RelayCommand]
        private void StopDownloadFileTask(object args)
        {
            if (args != null && args is DownloadFileTaskItem model)
            {
                if (model.State == 1)
                {
                    var key = $"{model.NodeName}#{model.FileName}";
                    if (_downloadTask.TryGetValue(key, out var handler))
                    {
                        handler.Stop();
                        _downloadTask.Remove(key);
                    }
                }
                else
                {
                    HandyControl.Controls.MessageBox.Show("当前不可以停止下载", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
        }

        [RelayCommand]
        private async Task StartDownloadFileTask(object args)
        {
            if (args != null && args is DownloadFileTaskItem model)
            {
                if (model.State == 0 ||
                    model.State == 2)
                {
                    await HandleDownloadFile(model.FileName, model.NodeName);
                }
                else
                {
                    HandyControl.Controls.MessageBox.Show("当前不可以开始下载", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
        }

        [RelayCommand]
        private async Task NodeSelectionChanged(object args)
        {
            await LoadData();
        }


        private async Task LoadData()
        {
            if (SendDataHolder!.Nodes.Count > NowIndex)
            {
                var matchedNode = SendDataHolder!.Nodes[NowIndex];
                if (matchedNode != null)
                {
                    var files = await matchedNode.GetCacheFiles();
                    GenerateTreeViewData(files, matchedNode.NodeName);
                }
            }
        }

        [RelayCommand]
        private async Task Loaded()
        {
            try
            {
                WeakReferenceMessenger.Default.Send(new MessageModel
                {
                    MessageType = MessageType.ShowMask
                });
                if (SendDataHolder!.Nodes.Count > 0)
                {
                    NowIndex = 0;
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"BrowserViewModel.Loaded error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                WeakReferenceMessenger.Default.Send(new MessageModel
                {
                    MessageType = MessageType.CloseMask
                });
            }
        }


        private void GenerateTreeViewData(List<FileItemInfo> files, string localNodeName)
        {
            FileNodes.Clear();
            if (files == null || files.Count == 0)
            {
                return;
            }
            try
            {
                foreach (var item in files)
                {
                    if (!string.IsNullOrEmpty(item.RelativePath))
                    {
                        RemoteNodeItem? parent = null;
                        var eachPart = item.RelativePath.Split(new string[] { "\\", "/" }, StringSplitOptions.None);
                        if (eachPart.Length >= 2)
                        {
                            for (int i = 0; i < eachPart.Length; i++)
                            {
                                if (i == 0)
                                {
                                    var node = FileNodes.FirstOrDefault(s => s.NodeName == eachPart[0]);
                                    if (node == null)
                                    {
                                        node = new RemoteNodeItem
                                        {
                                            NodeName = eachPart[0],
                                            Children = new ObservableCollection<RemoteNodeItem>(),
                                            FileSize = 0,
                                            LocalNodeName = localNodeName
                                        };
                                        FileNodes.Add(node);
                                    }
                                    parent = node;
                                }
                                else
                                {
                                    if (parent != null && parent.Children != null)
                                    {
                                        var eachNode = parent.Children.FirstOrDefault(s => s.NodeName == eachPart[i]);
                                        if (eachNode == null)
                                        {
                                            eachNode = new RemoteNodeItem
                                            {
                                                NodeName = eachPart[i],
                                                LocalNodeName = localNodeName
                                            };
                                            if (eachPart[i] != item.FileName)
                                            {
                                                eachNode.Children = new ObservableCollection<RemoteNodeItem>();
                                            }
                                            else
                                            {
                                                eachNode.FileSize = item.TotalSize;
                                            }
                                            parent.Children.Add(eachNode);
                                        }
                                        parent = eachNode;
                                    }
                                }
                            }
                        }
                        else
                        {
                            FileNodes.Add(new RemoteNodeItem
                            {
                                NodeName = item.FileName,
                                FileSize = item.TotalSize,
                                LocalNodeName = localNodeName
                            });
                        }
                    }
                    else
                    {
                        FileNodes.Add(new RemoteNodeItem
                        {
                            NodeName = item.FileName,
                            FileSize = item.TotalSize,
                            LocalNodeName = localNodeName
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"GenerateTreeViewData error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
            }
        }

        private void HandleFinishDownloadFile(DownloadFileTaskItem model)
        {
            _downloadTask.Remove($"{model.NodeName}#{model.FileName}");
        }

        private void HandleDownloadError(DownloadFileTaskItem model)
        {
            _downloadTask.Remove($"{model.NodeName}#{model.FileName}");
        }


        private async Task HandleDownloadFile(string fileName, string nodeName)
        {
            try
            {
                var node = SendDataHolder!.GetNode(nodeName: nodeName);//_nodeCaches.FirstOrDefault(s => s.NodeName == nodeName);
                if (node != null)
                {
                    if (node.State == 1)
                    {
                        Growl.Warning($"【{node.NodeName}】已经下线，无法下载【{fileName}】");
                        return;
                    }

                    var matchedTask = DownloadDataHolder!.Get(fileName);
                    if (matchedTask != null)
                    {
                        if (matchedTask.State == 0 ||
                            matchedTask.State == 1)
                        {
                            HandyControl.Controls.MessageBox.Show($"【{fileName}】已经在下载任务中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        else if (matchedTask.State == 3)
                        {
                            HandyControl.Controls.MessageBox.Show($"【{fileName}】已完成", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        else if (matchedTask.State == 4)
                        {
                            HandyControl.Controls.MessageBox.Show($"有发生错误的任务，请先移除", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                    }

                    var matchedFile = await node.GetCacheFile(fileName);
                    if (matchedFile != null)
                    {
                        if (matchedTask == null)
                        {
                            matchedTask = new DownloadFileTaskItem
                            {
                                FileName = matchedFile.FileName,
                                NodeName = nodeName,
                                TotalSize = matchedFile.TotalSize,
                                CurrentSize = 0,
                                Id = Guid.NewGuid().ToString()
                            };

                            DownloadDataHolder!.Add(matchedTask);
                        }
                        else
                        {
                            matchedTask.State = 1;
                        }
                        var key = $"{matchedTask.NodeName}#{matchedTask.FileName}";
                        if (_downloadTask.TryGetValue(key, out var handler))
                        {
                            handler.Cancel();
                            _downloadTask.Remove(key);
                        }
                        handler = new DownloadFileHandler(node, matchedTask);
                        _downloadTask.Add(key, handler);
                        _ = handler.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"HandleDownloadFile error, {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void Receive(MessageModel message)
        {
            switch (message.MessageType)
            {
                case MessageType.DownloadFile:
                    if (message.Data is DownloadTaskModel model)
                    {
                        _ = HandleDownloadFile(model.FileName, model.NodeName);
                    }
                    break;
                case MessageType.FinishDownloadFile:
                    if (message.Data is DownloadFileTaskItem finishModel)
                    {
                        HandleFinishDownloadFile(finishModel);
                    }
                    break;
                case MessageType.DownloadFileError:
                    if (message.Data is DownloadFileTaskItem errorModel)
                    {
                        HandleDownloadError(errorModel);
                    }
                    break;
            }
        }
    }
}
