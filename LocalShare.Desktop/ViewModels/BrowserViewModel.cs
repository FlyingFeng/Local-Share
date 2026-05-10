using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalShare.Desktop.KeepStates;
using LocalShare.Desktop.Models.Browsers;
using LocalShare.Protocol.Define;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.ViewModels
{
    public partial class BrowserViewModel : ObservableObject, IClosable
    {
        public ObservableCollection<RemoteNodeItem> FileNodes { get; set; } = [];
        public ObservableCollection<string> LocalNodes { get; set; } = [];

        private readonly SendDataHolder? _sendDataHolder;

        [ObservableProperty]
        private int selectedIndex = -1;

        public BrowserViewModel()
        {

        }

        public BrowserViewModel(SendDataHolder sendDataHolder)
        {
            _sendDataHolder = sendDataHolder;
        }

        public void Close()
        {
        }

        [RelayCommand]
        private void RefreshFile()
        {

        }


        [RelayCommand]
        private async Task Loaded()
        {
            try
            {
                var allNodes = _sendDataHolder!.GetAllNodes();
                if (allNodes.Count > 0)
                {
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
                    }
                    SelectedIndex = 0;
                    var matchedNode = allNodes[0];
                    var files = await matchedNode.GetCacheFiles();




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
                Dictionary<string, RemoteNodeItem> tmpFolderData = new Dictionary<string, RemoteNodeItem>();
                foreach (var item in files)
                {
                    if (!string.IsNullOrEmpty(item.RelativePath))
                    {

                    }
                    else
                    {
                        FileNodes.Add(new RemoteNodeItem
                        {
                            NodeName = item.FileName
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"GenerateTreeViewData error, {ex.Message}\n{ex.StackTrace}");
            }
        }


    }
}
