using CommonTool;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HandyControl.Controls;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Serilog;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Windows;

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
        private bool saveFilePathSelected;
        [ObservableProperty]
        private bool multicastAddressSelected = false;
        [ObservableProperty]
        private bool transferSpeedSelected = false;
        [ObservableProperty]
        private bool downloadSpeedSelected = false;


        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "【下载基准速率】不能为空")]
        [Range(1, 1024, ErrorMessage = "【下载基准速率】必须处于[1,1024]区间")]
        private string downloadSpeed = string.Empty;

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
        private string saveFilePath = string.Empty;


        public bool AnySelected => ServerPortSelected || BrocastPortSelected || SendNodeMaxCountSelected ||
                                    SameNodeMaxSendFileCountSelected || SaveFilePathSelected || MulticastAddressSelected ||
                                    TransferSpeedSelected;


        [RelayCommand]
        private void ChooseDownloadPath()
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            var flag = dialog.ShowDialog();
            if (flag == true)
            {
                SaveFilePath = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void GoToSaveFilePath()
        {
            try
            {
                if (Directory.Exists(SaveFilePath))
                {
                    ExplorerHelper.OpenFolder(SaveFilePath);
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show("跳转失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"LocalSettingViewModel.GoToSaveFilePath error, {ex.Message}\n{ex.StackTrace}");
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
                if (DownloadSpeedSelected)
                {
                    GlobalShared.DownloadSpeed = int.Parse(DownloadSpeed);
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
                if (SaveFilePathSelected)
                {
                    if (!string.IsNullOrEmpty(SaveFilePath) &&
                       !Directory.Exists(SaveFilePath))
                    {
                        try
                        {
                            Directory.CreateDirectory(SaveFilePath);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Create directory error, path: {SaveFilePath}, message: {ex.Message}");
                        }
                    }
                    GlobalShared.SaveFilePath = SaveFilePath;
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

                if (AnySelected)
                {
                    Growl.Info("应用成功");
                }
                else
                {
                    Growl.Warning("没有选中应用项");
                }
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (ValidateAll())
            {
                try
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
                    var saveFilePathEntity = settings.FirstOrDefault(s => s.Key == LocalSettingKey.KeySaveFilePath);
                    if (saveFilePathEntity != null)
                    {
                        saveFilePathEntity.Value = SaveFilePath;
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
                    var downloadSpeedEntity = settings.FirstOrDefault(s => s.Key == LocalSettingKey.KeyDownloadSpeed);
                    if (downloadSpeedEntity != null)
                    {
                        downloadSpeedEntity.Value = DownloadSpeed;
                    }
                    await dbContext.SaveChangesAsync();
                    Growl.Info("保存成功");
                }
                catch (Exception ex)
                {
                    Log.Error($"Save error, {ex.Message}\n{ex.StackTrace}");
                    HandyControl.Controls.MessageBox.Show($"保存失败\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void Loaded()
        {
            try
            {
                WeakReferenceMessenger.Default.Send(new MessageModel
                {
                    MessageType = MessageType.ShowMask
                });
                ServerPort = GlobalShared.ServerPort.ToString();
                BrocastPort = GlobalShared.BroadcastPort.ToString();
                SameNodeMaxSendFileCount = GlobalShared.SameNodeMaxSendFileCount.ToString();
                SendNodeMaxCount = GlobalShared.SendNodeMaxCount.ToString();
                SaveFilePath = GlobalShared.SaveFilePath!;
                MulticastAddress = GlobalShared.MulticastAddress!;
                TransferSpeed = GlobalShared.TransferSpeed.ToString();
                DownloadSpeed = GlobalShared.DownloadSpeed.ToString();
            }
            catch (Exception ex)
            {
                Log.Error($"LocalSettingViewModel.Loaded error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                WeakReferenceMessenger.Default.Send(new MessageModel
                {
                    MessageType = MessageType.CloseMask
                });
            }
        }


        private bool ValidateAll()
        {
            ValidateAllProperties();
            return !HasErrors;
        }


    }
}
