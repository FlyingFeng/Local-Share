using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using LocalShare.Desktop.KeepStates;
using LocalShare.Desktop.Models.Browsers;
using LocalShare.Desktop.Models.Sends;
using LocalShare.Protocol.Define;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LocalShare.Desktop.ViewModels
{
    public partial class BrowserViewModel : ObservableObject, IClosable
    {
        public ObservableCollection<RemoteNodeItem> FileNodes { get; set; } = [];
        public ObservableCollection<string> LocalNodes { get; set; } = [];

        private readonly SendDataHolder? _sendDataHolder;
        private readonly List<LocalNode> _nodeCaches = new List<LocalNode>();

        [ObservableProperty]
        private int selectedIndex = -1;

        public DownloadDataHolder? DownloadDataHolder { get; set; }

        public BrowserViewModel()
        {

        }

        public BrowserViewModel(SendDataHolder? sendDataHolder, DownloadDataHolder? downloadDataHolder)
        {
            _sendDataHolder = sendDataHolder;
            DownloadDataHolder = downloadDataHolder;
        }

        public void Close()
        {
        }

        [RelayCommand]
        private async Task RefreshFile()
        {
            try
            {
                if (SelectedIndex >= 0 && SelectedIndex < _nodeCaches.Count)
                {
                    var matchedNode = _nodeCaches[SelectedIndex];
                    if (matchedNode.State == 1)
                    {
                        HandyControl.Controls.MessageBox.Show("节点已经离线", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    var files = await matchedNode.RefreshCacheFiles();
                    GenerateTreeViewData(files);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RefreshFile error, {ex.Message}\n{ex.StackTrace}");
            }
        }


        [RelayCommand]
        private async Task Loaded()
        {
            try
            {
                var allNodes = _sendDataHolder!.GetAllNodes();
                if (allNodes.Count > 0)
                {
                    _nodeCaches.Clear();
                    LocalNodes.Clear();
                    foreach (var item in allNodes)
                    {
                        if (item.State == 0) //onlone
                        {
                            LocalNodes.Add(item.NodeName);
                        }
                        else //offline
                        {
                            LocalNodes.Add($"{item.NodeName} - 离线");
                        }
                        _nodeCaches.Add(item);
                    }
                    SelectedIndex = 0;
                    var matchedNode = allNodes[0];
                    var files = await matchedNode.GetCacheFiles();
                    GenerateTreeViewData(files);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"BrowserViewModel.Loaded error, {ex.Message}\n{ex.StackTrace}");
            }
        }


        private void GenerateTreeViewData(List<FileItemInfo> files)
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
                                            FileSize = 0
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
                                                NodeName = eachPart[i]
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
                                FileSize = item.TotalSize
                            });
                        }
                    }
                    else
                    {
                        FileNodes.Add(new RemoteNodeItem
                        {
                            NodeName = item.FileName,
                            FileSize = item.TotalSize
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

        [RelayCommand]
        private void DownloadFile(object args)
        {
            HandyControl.Controls.MessageBox.Show(999.ToString());
        }


    }
}
