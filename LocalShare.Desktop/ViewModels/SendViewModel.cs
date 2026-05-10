using CommonTool;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.KeepStates;
using LocalShare.Desktop.Models;
using LocalShare.Desktop.Models.Sends;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using MessageBox = HandyControl.Controls.MessageBox;

namespace LocalShare.Desktop.ViewModels
{
    public partial class SendViewModel : ObservableObject, IClosable, IRecipient<MessageModel>
    {
        public SendViewModel() { }
        private readonly UdpMulticastDiscoveryService? _udpDiscoveryService;
        public SendViewModel(UdpMulticastDiscoveryService udpDiscoveryService,
            SendDataHolder sendDataHolder
            )
        {
            _udpDiscoveryService = udpDiscoveryService;
            _udpDiscoveryService.ClientDiscovered += UdpDiscoveryService_ClientDiscovered;
            SendDataHolder = sendDataHolder;
            WeakReferenceMessenger.Default.Register<MessageModel>(this);
        }


        public ObservableCollection<FileTaskModel> CurrentNodeFileTasks { get; set; } = [];

        public SendDataHolder? SendDataHolder { get; set; }
        [ObservableProperty]
        private bool isSelectAll;
        [ObservableProperty]
        private bool isNodeSelectAll;
        [ObservableProperty]
        private int selectedNodeIndex = -1;

        [RelayCommand]
        private async Task AddLocalNode()
        {
            try
            {
                AddLocalNodeWindow window = new AddLocalNodeWindow();
                var flag = window.ShowDialog();
                if (flag == true)
                {
                    if (window.NodeInfo != null)
                    {
                        var matched = SendDataHolder!.GetNode(ipAddress: window.NodeInfo.IpAddress, port: window.NodeInfo.Port);//HomeDataHolder!.Nodes.FirstOrDefault(s => s.IpAddress == window.NodeInfo.IpAddress && s.Port == window.NodeInfo.Port);
                        if (matched == null)
                        {
                            var node = new LocalNode()
                            {
                                InBlackList = false,
                                InWhiteList = false,
                                IsSelected = false,
                                NodeName = window.NodeInfo.NodeName,
                                Port = window.NodeInfo.Port,
                                IpAddress = window.NodeInfo.IpAddress,
                                LastSeenTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                            };
                            await node.InitAsync();
                            SendDataHolder!.Nodes.Add(node);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加节点失败\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"SendViewModel.AddLocalNode error, {ex.Message}\n{ex.StackTrace}");
            }
        }

        [RelayCommand]
        private async Task Loaded()
        {
            try
            {
                if (SendDataHolder != null)
                {
                    using var _dbContext = new LocalDataContext();
                    var files = await _dbContext!.LocalFiles.ToListAsync() ?? [];
                    files.ForEach((e) =>
                    {
                        var matched = SendDataHolder.FileCaches.FirstOrDefault(s => s.FilePath == e.FileFullPath);
                        if (matched == null)
                        {
                            SendDataHolder.FileCaches.Add(new FileCache
                            {
                                FileName = e.FileName,
                                FilePath = e.FileFullPath,
                                FileSize = e.FileSize,
                                IsSelected = false,
                                Md5 = e.MD5,
                                IsOpenFromDir = e.IsOpenFromDir
                            });
                        }
                    });

                    var nodeCaches = _udpDiscoveryService?.GetNodeModelCaches() ?? [];
                    foreach (var e in nodeCaches)
                    {
                        var matched = SendDataHolder!.GetNode(ipAddress: e.IpAddress, port: e.Port);
                        if (matched == null)
                        {
                            var node = new LocalNode()
                            {
                                InBlackList = false,
                                InWhiteList = false,
                                IsSelected = false,
                                NodeName = e.NodeName,
                                IpAddress = e.IpAddress,
                                Port = e.Port,
                                LastSeenTime = e.Time
                            };
                            SendDataHolder!.AddNode(node);
                            await node.InitAsync();
                        }
                    }

                    if (SendDataHolder.Nodes!.Count > 0)
                    {
                        var firstNode = SendDataHolder.Nodes.First();
                        CurrentNodeFileTasks.Clear();
                        foreach (var item in firstNode.FileTasks)
                        {
                            CurrentNodeFileTasks.Add(item);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Error($"SendViewModel.Loaded error, {ex.Message}\n{ex.StackTrace}");
            }
        }

        [RelayCommand]
        private async Task RemoveSelectedFile()
        {
            try
            {
                var selected = SendDataHolder!.FileCaches.Where(s => s.IsSelected).ToList();
                if (selected.Count > 0)
                {
                    using var _dbContext = new LocalDataContext();
                    foreach (var item in selected)
                    {
                        SendDataHolder!.FileCaches.Remove(item);
                        var matched = _dbContext!.LocalFiles.FirstOrDefault(s => s.FileFullPath == item.FilePath);
                        if (matched != null)
                        {
                            _dbContext.LocalFiles.Remove(matched);
                        }
                    }
                    await _dbContext!.SaveChangesAsync();
                    IsSelectAll = false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"SendViewModel.RemoveSelectedFile error, {ex.Message}\n{ex.StackTrace}");
            }
        }

        [RelayCommand]
        private void OpenDirectory(object parameter)
        {
            try
            {
                if (parameter is string str && File.Exists(str))
                {
                    FileInfo fi = new FileInfo(str);
                    if (Directory.Exists(fi.DirectoryName))
                    {
                        ExplorerHelper.OpenFolder(fi.DirectoryName);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"SendViewModel.OpenDirectory error, {ex.Message}\n{ex.StackTrace}");
            }
        }

        [RelayCommand]
        private async Task OpenDirFiles()
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            var flag = dialog.ShowDialog();
            if (flag == true &&
                !string.IsNullOrEmpty(dialog.FolderName) &&
                Directory.Exists(dialog.FolderName))
            {
                try
                {
                    WeakReferenceMessenger.Default.Send(new MessageModel
                    {
                        MessageType = MessageType.ShowMask
                    });
                    DirectoryInfo directoryInfo = new DirectoryInfo(dialog.FolderName);
                    var files = directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories);
                    if (files.Any())
                    {
                        using var _dbContext = new LocalDataContext();
                        var fileEntities = _dbContext!.LocalFiles.ToList() ?? [];
                        foreach (var info in files)
                        {
                            try
                            {
                                var md5 = string.Empty; //await FileHashHelper.ComputeMd5Async(info.FullName);
                                var matchedEntity = fileEntities.FirstOrDefault(s => s.FileFullPath == info.FullName);
                                if (matchedEntity == null)
                                {
                                    await _dbContext.LocalFiles.AddAsync(new DataContext.Entities.LocalFileEntity
                                    {
                                        FileExt = info.Extension,
                                        FileFullPath = info.FullName,
                                        FileName = info.Name,
                                        FileSize = info.Length,
                                        InitTime = DateTime.UtcNow,
                                        MD5 = md5,
                                        IsOpenFromDir = true
                                    });
                                }

                                var matched = SendDataHolder!.FileCaches.FirstOrDefault(s => s.FilePath == info.FullName);
                                if (matched == null)
                                {
                                    matched = new FileCache
                                    {
                                        FileName = info.Name,
                                        FilePath = info.FullName,
                                        FileSize = info.Length,
                                        IsSelected = false,
                                        Md5 = md5,
                                        IsOpenFromDir = true
                                    };
                                    SendDataHolder!.FileCaches.Add(matched);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Open {info.FullName} error, {ex.Message}\n{ex.StackTrace}");
                            }
                        }
                        await _dbContext.SaveChangesAsync();
                    }
                }
                finally
                {
                    WeakReferenceMessenger.Default.Send(new MessageModel
                    {
                        MessageType = MessageType.CloseMask
                    });
                }
            }
        }

        [RelayCommand]
        private async Task OpenFiles()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            var flag = dialog.ShowDialog();
            if (flag == true && dialog.FileNames.Length > 0)
            {
                try
                {
                    WeakReferenceMessenger.Default.Send(new MessageModel
                    {
                        MessageType = MessageType.ShowMask
                    });

                    using var _dbContext = new LocalDataContext();
                    var fileEntities = _dbContext!.LocalFiles.ToList() ?? [];
                    foreach (var file in dialog.FileNames)
                    {
                        try
                        {
                            var info = new FileInfo(file);
                            var md5 = string.Empty; //await FileHashHelper.ComputeMd5Async(info.FullName);
                            var matchedEntity = fileEntities.FirstOrDefault(s => s.FileFullPath == file);
                            if (matchedEntity == null)
                            {
                                await _dbContext.LocalFiles.AddAsync(new DataContext.Entities.LocalFileEntity
                                {
                                    FileExt = info.Extension,
                                    FileFullPath = info.FullName,
                                    FileName = info.Name,
                                    FileSize = info.Length,
                                    InitTime = DateTime.UtcNow,
                                    MD5 = md5,
                                    IsOpenFromDir = false
                                });
                            }

                            var matched = SendDataHolder!.FileCaches.FirstOrDefault(s => s.FilePath == info.FullName);
                            if (matched == null)
                            {
                                matched = new FileCache
                                {
                                    FileName = info.Name,
                                    FilePath = info.FullName,
                                    FileSize = info.Length,
                                    IsSelected = false,
                                    Md5 = md5,
                                    IsOpenFromDir = false
                                };
                                SendDataHolder!.FileCaches.Add(matched);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Open {file} error, {ex.Message}\n{ex.StackTrace}");
                        }
                    }
                    await _dbContext.SaveChangesAsync();

                }
                finally
                {
                    WeakReferenceMessenger.Default.Send(new MessageModel
                    {
                        MessageType = MessageType.CloseMask
                    });
                }
            }
        }

        [RelayCommand]
        private void EachCheckedChanged(object parameter)
        {
            if (parameter is bool b)
            {
                if (!b)
                {
                    IsSelectAll = false;
                }
                else
                {
                    if (SendDataHolder!.FileCaches.All(s => s.IsSelected))
                    {
                        IsSelectAll = true;
                    }
                }
            }
        }

        [RelayCommand]
        private void EachNodeCheckedChanged(object parameter)
        {
            if (parameter is bool b)
            {
                if (!b)
                {
                    IsNodeSelectAll = false;
                }
                else
                {
                    if (SendDataHolder!.Nodes.All(s => s.IsSelected))
                    {
                        IsNodeSelectAll = true;
                    }
                }
            }
        }


        [RelayCommand]
        private void GlobalCheckedChanged(object parameter)
        {
            if (parameter is bool b)
            {
                foreach (var item in SendDataHolder!.FileCaches)
                {
                    item.IsSelected = b;
                }
            }
        }

        [RelayCommand]
        private void GlobalNodeCheckedChanged(object parameter)
        {
            if (parameter is bool b)
            {
                foreach (var item in SendDataHolder!.Nodes)
                {
                    item.IsSelected = b;
                }
            }
        }

        [RelayCommand]
        private void SendFile()
        {
            var selectedFiles = SendDataHolder!.FileCaches.Where(s => s.IsSelected).ToList();
            var selectedNodes = SendDataHolder.Nodes.Where(s => s.IsSelected).ToList();
            if (selectedNodes.Count > GlobalShared.SendNodeMaxCount)
            {
                HandyControl.Controls.MessageBox.Show($"最多只能选择{GlobalShared.SendNodeMaxCount}个节点", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (selectedFiles.Count > GlobalShared.SameNodeMaxSendFileCount)
            {
                HandyControl.Controls.MessageBox.Show($"每个节点最多只能选择{GlobalShared.SameNodeMaxSendFileCount}个文件", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            var sendFileNodeCount = SendDataHolder!.Nodes.Where(s => s.IsSending).Count();
            if (sendFileNodeCount > GlobalShared.SendNodeMaxCount)
            {
                HandyControl.Controls.MessageBox.Show($"同时发送文件的节点最多只能有{GlobalShared.SendNodeMaxCount}个", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            foreach (var eachFile in selectedFiles)
            {
                if (!File.Exists(eachFile.FilePath))
                {
                    MessageBox.Show($"文件不存在: 【{eachFile.FilePath}】", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            foreach (var eachNode in selectedNodes)
            {
                foreach (var eachFile in selectedFiles)
                {
                    var matched = eachNode.FileTasks.FirstOrDefault(s => s.FileName == eachFile.FileName);
                    if (matched == null)
                    {
                        eachNode.FileTasks.Add(new FileTaskModel
                        {
                            FileName = eachFile.FileName,
                            IsOpenFromDir = eachFile.IsOpenFromDir,
                            Md5 = eachFile.Md5,
                            TotalSize = eachFile.FileSize,
                            State = 0,
                            FullFileName = eachFile.FilePath,
                            Parent = eachNode,
                            TaskId = Guid.NewGuid().ToString()
                        });
                    }
                }
            }
            if (SendDataHolder.Nodes.Count > 0)
            {
                SelectedNodeIndex = -1;
                SelectedNodeIndex = 0;
            }
        }

        [RelayCommand]
        private void NodeSelectionChanged(object? args)
        {
            if (args != null && args is LocalNode node)
            {
                CurrentNodeFileTasks.Clear();
                foreach (var item in node.FileTasks)
                {
                    CurrentNodeFileTasks.Add(item);
                }
            }
        }

        private bool isHandle = false;
        private async void UdpDiscoveryService_ClientDiscovered(Protocol.Define.NodeModel obj)
        {
            if (isHandle)
            {
                return;
            }
            try
            {
                isHandle = true;

                var matched = SendDataHolder!.GetNode(obj.IpAddress, obj.Port);
                if (matched == null)
                {
                    var node = new LocalNode()
                    {
                        InBlackList = false,
                        InWhiteList = false,
                        IsSelected = false,
                        NodeName = obj.NodeName,
                        Port = obj.Port,
                        IpAddress = obj.IpAddress,
                        LastSeenTime = obj.Time
                    };
                    SendDataHolder!.AddNode(node);
                    await node.InitAsync();
                }
                else
                {
                    matched.NodeName = obj.NodeName;
                    matched.LastSeenTime = obj.Time;
                    //await matched.UpdateNodeStateAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"UdpDiscoveryService_ClientDiscovered error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                isHandle = false;
            }
        }

        public void Close()
        {
            // 取消订阅，切断单例对本对象的引用
            _udpDiscoveryService!.ClientDiscovered -= UdpDiscoveryService_ClientDiscovered;
        }

        public void Receive(MessageModel message)
        {
            if (message.MessageType == MessageType.RemoveCurrentNodeFinishedSendFileTask)
            {
                if (message.Data is string fileName)
                {
                    var matched = CurrentNodeFileTasks.FirstOrDefault(s => s.FileName == fileName);
                    if (matched != null)
                    {
                        CurrentNodeFileTasks.Remove(matched);
                    }
                }
            }
        }
    }
}
