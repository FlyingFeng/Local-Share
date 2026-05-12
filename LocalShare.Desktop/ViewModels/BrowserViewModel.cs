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

namespace LocalShare.Desktop.ViewModels
{
    public partial class BrowserViewModel : ObservableObject, IClosable, IRecipient<MessageModel>
    {
        public ObservableCollection<RemoteNodeItem> FileNodes { get; set; } = [];
        public ObservableCollection<string> LocalNodes { get; set; } = [];

        private readonly SendDataHolder? _sendDataHolder;
        private readonly List<LocalNode> _nodeCaches = [];
        private readonly Dictionary<string, DownloadFileHandler> _downloadTask = new Dictionary<string, DownloadFileHandler>();


        [ObservableProperty]
        private int selectedIndex = -1;


        public BrowserViewModel() { }

        public BrowserViewModel(SendDataHolder? sendDataHolder, DownloadDataHolder? downloadDataHolder)
        {
            _sendDataHolder = sendDataHolder;
            DownloadDataHolder = downloadDataHolder;
            WeakReferenceMessenger.Default.Register<MessageModel>(this);
        }
        public DownloadDataHolder? DownloadDataHolder { get; set; }

        public void Close()
        {
            WeakReferenceMessenger.Default.Unregister<MessageModel>(this);
        }

        [RelayCommand]
        private async Task RefreshFile()
        {
            try
            {
                await LoadData();
            }
            catch (Exception ex)
            {
                Log.Error($"RefreshFile error, {ex.Message}\n{ex.StackTrace}");
            }
        }


        private async Task LoadData()
        {
            var allNodes = _sendDataHolder!.GetAllNodes();
            if (allNodes.Count > 0)
            {
                foreach (var item in allNodes)
                {
                    var str = item.NodeName;
                    if (item.State == 1)
                    {
                        str += " - 离线";
                    }
                    var matched = _nodeCaches.FirstOrDefault(s => s.NodeName == item.NodeName);
                    if (matched == null)
                    {
                        _nodeCaches.Add(item);
                    }
                    var nameMatched = LocalNodes.FirstOrDefault(s => s == str);
                    if (nameMatched == null)
                    {
                        LocalNodes.Add(str);
                    }
                }
                SelectedIndex = 0;
                var matchedNode = allNodes[0];
                var files = await matchedNode.GetCacheFiles();
                GenerateTreeViewData(files, matchedNode.NodeName);
            }
        }

        [RelayCommand]
        private async Task Loaded()
        {
            try
            {
                await LoadData();
            }
            catch (Exception ex)
            {
                Log.Error($"BrowserViewModel.Loaded error, {ex.Message}\n{ex.StackTrace}");
            }
        }


        private void GenerateTreeViewData(List<FileItemInfo> files, string localNodeName)
        {
            if (files == null || files.Count == 0)
            {
                return;
            }
            try
            {
                FileNodes.Clear();
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

        private async Task HandleDownloadFile(DownloadTaskModel model)
        {
            try
            {
                var node = _nodeCaches.FirstOrDefault(s => s.NodeName == model.NodeName);
                if (node != null)
                {
                    if (node.State == 1)
                    {
                        Growl.Warning($"【{node.NodeName}】已经下线，无法下载【{model.FileName}】");
                        return;
                    }

                    var existed = DownloadDataHolder!.Exist(model.FileName);
                    if (existed)
                    {
                        Growl.Warning($"【{model.FileName}】已经在下载中");
                        return;
                    }

                    var matchedFile = await node.GetCacheFile(model.FileName);
                    if (matchedFile != null)
                    {
                        var item = new DownloadFileTaskItem
                        {
                            FileName = matchedFile.FileName,
                            NodeName = model.NodeName,
                            TotalSize = matchedFile.TotalSize,
                            CurrentSize = 0,
                            Id = Guid.NewGuid().ToString()
                        };
                        DownloadFileHandler handler = new DownloadFileHandler(node, item);
                        DownloadDataHolder!.Add(item);
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
            if (message.MessageType != MessageType.DownloadFile)
            {
                return;
            }
            if (message.Data is DownloadTaskModel model)
            {
                _ = HandleDownloadFile(model);
            }

        }
    }
}
