using CommonTool;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HandyControl.Controls;
using HandyControl.Data;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.KeepStates;
using LocalShare.Desktop.Models;
using LocalShare.Desktop.Models.Sends;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace LocalShare.Desktop.ViewModels
{
    public partial class SendViewModel : ObservableObject, IClosable, IRecipient<MessageModel>
    {
        public SendViewModel() { }
        //private readonly LocalDataContext? _dbContext;
        private readonly UdpDiscoveryService? _udpDiscoveryService;
        private readonly IServiceProvider? _serviceProvider;
        public SendViewModel(UdpDiscoveryService udpDiscoveryService,
            SendDataHolder homeDataHolder,
            IServiceProvider serviceProvider)
        {
            //_dbContext = localDataContext;
            _udpDiscoveryService = udpDiscoveryService;
            _udpDiscoveryService.ClientDiscovered += UdpDiscoveryService_ClientDiscovered;
            HomeDataHolder = homeDataHolder;
            _serviceProvider = serviceProvider;
            WeakReferenceMessenger.Default.Register<MessageModel>(this);
        }


        public ObservableCollection<FileTaskModel> CurrentNodeFileTasks { get; set; } = [];

        public SendDataHolder? HomeDataHolder { get; set; }
        [ObservableProperty]
        private bool isSelectAll;
        [ObservableProperty]
        private bool isNodeSelectAll;
        [ObservableProperty]
        private int selectedNodeIndex = -1;

        [RelayCommand]
        private void AddLocalNode()
        {

        }

        [RelayCommand]
        private async Task Loaded()
        {
            try
            {
                if (HomeDataHolder != null)
                {
                    using var _dbContext = new LocalDataContext();
                    var files = await _dbContext!.LocalFiles.ToListAsync() ?? [];
                    files.ForEach((e) =>
                    {
                        var matched = HomeDataHolder.FileCaches.FirstOrDefault(s => s.FilePath == e.FileFullPath);
                        if (matched == null)
                        {
                            HomeDataHolder.FileCaches.Add(new FileCache
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
                    nodeCaches.ForEach(async e =>
                    {
                        var matched = HomeDataHolder.Nodes.FirstOrDefault(s => e.NodeName == s.NodeName);
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
                            await node.InitAsync();
                            HomeDataHolder.Nodes.Add(node);
                        }
                    });

                    if (HomeDataHolder.Nodes!.Count > 0)
                    {
                        var firstNode = HomeDataHolder.Nodes.First();
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
        private void RemoveSelectedNode()
        {
            Growl.Info("test");
        }

        [RelayCommand]
        private async Task RemoveSelectedFile()
        {
            try
            {
                var selected = HomeDataHolder!.FileCaches.Where(s => s.IsSelected).ToList();
                if (selected.Count > 0)
                {
                    using var _dbContext = new LocalDataContext();
                    foreach (var item in selected)
                    {
                        HomeDataHolder!.FileCaches.Remove(item);
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
                                var md5 = await FileHashHelper.ComputeMd5Async(info.FullName);
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

                                var matched = HomeDataHolder!.FileCaches.FirstOrDefault(s => s.FilePath == info.FullName);
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
                                    HomeDataHolder!.FileCaches.Add(matched);
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
                            var md5 = await FileHashHelper.ComputeMd5Async(info.FullName);
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

                            var matched = HomeDataHolder!.FileCaches.FirstOrDefault(s => s.FilePath == info.FullName);
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
                                HomeDataHolder!.FileCaches.Add(matched);
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
                    if (HomeDataHolder!.FileCaches.All(s => s.IsSelected))
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
                    if (HomeDataHolder!.Nodes.All(s => s.IsSelected))
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
                foreach (var item in HomeDataHolder!.FileCaches)
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
                foreach (var item in HomeDataHolder!.Nodes)
                {
                    item.IsSelected = b;
                }
            }
        }

        [RelayCommand]
        private void SendFile()
        {
            var selectedFiles = HomeDataHolder!.FileCaches.Where(s => s.IsSelected).ToList();
            var selectedNodes = HomeDataHolder.Nodes.Where(s => s.IsSelected).ToList();
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
            var sendFileNodeCount = HomeDataHolder!.Nodes.Where(s => s.IsSending).Count();
            if (sendFileNodeCount > GlobalShared.SendNodeMaxCount)
            {
                HandyControl.Controls.MessageBox.Show($"同时发送文件的节点最多只能有{GlobalShared.SendNodeMaxCount}个", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
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
                            Parent = eachNode
                        });
                    }
                }
            }
            if (HomeDataHolder.Nodes.Count > 0)
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
                //CurrentNodeFileTasks = node.FileTasks;
                CurrentNodeFileTasks.Clear();
                foreach (var item in node.FileTasks)
                {
                    CurrentNodeFileTasks.Add(item);
                }
            }
        }


        private async void UdpDiscoveryService_ClientDiscovered(Protocol.Define.NodeModel obj)
        {
            var matched = HomeDataHolder!.Nodes.FirstOrDefault(s => s.IpAddress == obj.IpAddress && s.Port == obj.Port);
            if (matched == null)
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
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
                     await node.InitAsync();
                     HomeDataHolder!.Nodes.Add(node);
                 });
            }
            else
            {
                matched.NodeName = obj.NodeName;
                matched.LastSeenTime = obj.Time;
                await matched.UpdateNodeStateAsync();
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
