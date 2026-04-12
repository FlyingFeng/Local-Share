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
    public partial class SendViewModel : ObservableObject, IClosable
    {
        public SendViewModel() { }
        private readonly LocalDataContext? _dbContext;
        private readonly UdpDiscoveryService? _udpDiscoveryService;
        private readonly IServiceProvider? _serviceProvider;
        public SendViewModel(LocalDataContext localDataContext,
            UdpDiscoveryService udpDiscoveryService,
            HomeDataHolder homeDataHolder,
            IServiceProvider serviceProvider)
        {
            _dbContext = localDataContext;
            _udpDiscoveryService = udpDiscoveryService;
            _udpDiscoveryService.ClientDiscovered += UdpDiscoveryService_ClientDiscovered;
            HomeDataHolder = homeDataHolder;
            _serviceProvider = serviceProvider;
        }

        public HomeDataHolder? HomeDataHolder { get; set; }
        [ObservableProperty]
        private bool isSelectAll;
        [ObservableProperty]
        private bool isNodeSelectAll;

        [RelayCommand]
        private async Task Loaded()
        {
            try
            {
                if (HomeDataHolder != null)
                {
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
                            var node = new LocalNode(_serviceProvider!)
                            {
                                InBlackList = false,
                                InWhiteList = false,
                                IsSelected = false,
                                NodeName = e.NodeName,
                                IpAddress = e.IpAddress,
                                Port = e.Port,
                                LastSeenTime = e.Time,
                                FileTasks = new ObservableCollection<FileTaskModel>()
                            };
                            await node.InitAsync();
                            HomeDataHolder.Nodes.Add(node);
                        }
                    });
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


        private void UdpDiscoveryService_ClientDiscovered(Protocol.Define.NodeModel obj)
        {
            var matched = HomeDataHolder!.Nodes.FirstOrDefault(s => s.IpAddress == obj.IpAddress && s.Port == obj.Port);
            if (matched == null)
            {
                Application.Current.Dispatcher.Invoke(async () =>
                {
                    var node = new LocalNode(_serviceProvider!)
                    {
                        InBlackList = false,
                        InWhiteList = false,
                        IsSelected = false,
                        NodeName = obj.NodeName,
                        Port = obj.Port,
                        IpAddress = obj.IpAddress,
                        FileTasks = [],
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
            }
        }

        public void Close()
        {
            // 取消订阅，切断单例对本对象的引用
            _udpDiscoveryService!.ClientDiscovered -= UdpDiscoveryService_ClientDiscovered;
        }
    }
}
