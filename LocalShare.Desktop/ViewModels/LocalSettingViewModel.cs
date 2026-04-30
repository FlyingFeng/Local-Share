using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HandyControl.Controls;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.ViewModels
{
    public partial class LocalSettingViewModel : ObservableValidator, IClosable
    {

        public LocalSettingViewModel()
        {

        }

        public void Close()
        {
        }

        [ObservableProperty]
        private bool selectedAll;
        [ObservableProperty]
        private bool serverPortSelected;
        [ObservableProperty]
        private bool brocastPortSelected;
        [ObservableProperty]
        private bool sendNodeMaxCountSelected;
        [ObservableProperty]
        private bool sameNodeMaxSendFileCountSelected;
        [ObservableProperty]
        private bool downloadPathSelected;
        [ObservableProperty]
        private bool multicastAddressSelected = false;
        [ObservableProperty]
        private bool transferSpeedSelected = false;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "【传输基准速率】不能为空")]
        [Range(1, 1024, ErrorMessage = "【传输基准速率】必须处于[1,1024]区间")]
        private string transferSpeed = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [RegularExpression(@"^(22[4-9]|23[0-9])\.(25[0-5]|2[0-4]\d|1\d{2}|[1-9]\d|\d)\.(25[0-5]|2[0-4]\d|1\d{2}|[1-9]\d|\d)\.(25[0-5]|2[0-4]\d|1\d{2}|[1-9]\d|\d)", ErrorMessage = "【组播地址】必须是合法的IP地址")]
        [Required(ErrorMessage = "【组播地址】不能为空")]
        private string multicastAddress = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "【监听端口】不能为空")]
        [Range(10000, 19999, ErrorMessage = "【监听端口】必须处于[10000,19999]区间")]
        private string serverPort = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "【广播端口】不能为空")]
        [Range(5000, 9999, ErrorMessage = "【广播端口】必须处于[5000,9999]区间")]
        private string brocastPort = string.Empty;


        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "【同时发文件送节点数】不能为空")]
        [Range(1, 5, ErrorMessage = "【同时发文件送节点数】必须处于[1,5]区间")]
        private string sendNodeMaxCount = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "【节点同时发送文件数】不能为空")]
        [Range(1, 5, ErrorMessage = "【节点同时发送文件数】必须处于[1,5]区间")]
        private string sameNodeMaxSendFileCount = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "【文件保存路径】不能为空")]
        private string downloadPath = string.Empty;

        [RelayCommand]
        private void ChooseDownloadPath()
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            var flag = dialog.ShowDialog();
            if (flag == true)
            {
                DownloadPath = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void Apply()
        {
            if (ValidateAll())
            {
                if (ServerPortSelected)
                {
                    if (int.TryParse(ServerPort, out var newPort) && GlobalShared.ServerPort != newPort)
                    {
                        GlobalShared.ServerPort = int.Parse(ServerPort);
                        WeakReferenceMessenger.Default.Send(new MessageModel
                        {
                            MessageType = MessageType.RestartServer
                        });
                    }
                }
                if (BrocastPortSelected)
                {
                    if (int.TryParse(BrocastPort, out var newPort) && GlobalShared.BroadcastPort != newPort)
                    {
                        GlobalShared.BroadcastPort = int.Parse(BrocastPort);
                        WeakReferenceMessenger.Default.Send(new MessageModel
                        {
                            MessageType = MessageType.RestartBrocast
                        });
                    }
                }
                if (SendNodeMaxCountSelected)
                {
                    GlobalShared.SendNodeMaxCount = int.Parse(SendNodeMaxCount);
                }
                if (SameNodeMaxSendFileCountSelected)
                {
                    GlobalShared.SameNodeMaxSendFileCount = int.Parse(SameNodeMaxSendFileCount);
                }
                if (TransferSpeedSelected)
                {
                    GlobalShared.TransferSpeed = int.Parse(TransferSpeed);
                }
                if (DownloadPathSelected)
                {
                    if (!string.IsNullOrEmpty(DownloadPath) &&
                       !Directory.Exists(DownloadPath))
                    {
                        try
                        {
                            Directory.CreateDirectory(DownloadPath);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Create directory error, path: {DownloadPath}, message: {ex.Message}");
                        }
                    }
                    GlobalShared.DownloadPath = DownloadPath;
                }
                if (MulticastAddressSelected)
                {
                    if (!string.IsNullOrEmpty(MulticastAddress) && GlobalShared.MulticastAddress != MulticastAddress)
                    {
                        GlobalShared.MulticastAddress = MulticastAddress;
                    }
                    WeakReferenceMessenger.Default.Send(new MessageModel
                    {
                        MessageType = MessageType.RestartMulticast
                    });
                }

                Growl.Info("应用成功");
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (ValidateAll())
            {
                using var dbContext = new LocalDataContext();
                var settings = await dbContext.LocalSettings.ToListAsync();
                var serverPortEntity = settings.FirstOrDefault(s => s.Key == LocalSettingKey.KeyServerPort);
                if (serverPortEntity != null)
                {
                    serverPortEntity.Value = ServerPort;
                }
                var brocastPortEntity = settings.FirstOrDefault(s => s.Key == LocalSettingKey.KeyBrocastPort);
                if (brocastPortEntity != null)
                {
                    brocastPortEntity.Value = BrocastPort;
                }
                var sendFileNodeCountEntity = settings.FirstOrDefault(s => s.Key == LocalSettingKey.KeySendNodeMaxCount);
                if (sendFileNodeCountEntity != null)
                {
                    sendFileNodeCountEntity.Value = SendNodeMaxCount;
                }
                var sameNodeSendFileCountEntity = settings.FirstOrDefault(s => s.Key == LocalSettingKey.KeySameNodeMaxSendFileCount);
                if (sameNodeSendFileCountEntity != null)
                {
                    sameNodeSendFileCountEntity.Value = SameNodeMaxSendFileCount;
                }
                var downloadPathEntity = settings.FirstOrDefault(s => s.Key == LocalSettingKey.KeyDownloadPath);
                if (downloadPathEntity != null)
                {
                    downloadPathEntity.Value = DownloadPath;
                }
                var multicastAddressEntity = settings.FirstOrDefault(s => s.Key == LocalSettingKey.KeyMulticastAddress);
                if (multicastAddressEntity != null)
                {
                    multicastAddressEntity.Value = MulticastAddress;
                }
                var transferSpeedEntity = settings.FirstOrDefault(s => s.Key == LocalSettingKey.KeyTransferSpeed);
                if (transferSpeedEntity != null)
                {
                    transferSpeedEntity.Value = TransferSpeed;
                }
                await dbContext.SaveChangesAsync();
                Growl.Info("保存成功");
            }
        }

        [RelayCommand]
        private void Loaded()
        {
            ServerPort = GlobalShared.ServerPort.ToString();
            BrocastPort = GlobalShared.BroadcastPort.ToString();
            SameNodeMaxSendFileCount = GlobalShared.SameNodeMaxSendFileCount.ToString();
            SendNodeMaxCount = GlobalShared.SendNodeMaxCount.ToString();
            DownloadPath = GlobalShared.DownloadPath!;
            MulticastAddress = GlobalShared.MulticastAddress!;
            TransferSpeed = GlobalShared.TransferSpeed.ToString();
        }


        private bool ValidateAll()
        {
            ValidateAllProperties();
            return !HasErrors;
        }


    }
}
