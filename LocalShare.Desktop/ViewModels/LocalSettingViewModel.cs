using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "【监听端口】不能为空")]
        [Range(10000, 19999, ErrorMessage = "【监听端口】必须处于[10000,19999]区间")]
        private string serverPort;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "【广播端口】不能为空")]
        [Range(5000, 9999, ErrorMessage = "【广播端口】必须处于[5000,9999]区间")]
        private string brocastPort;


        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "【同时发文件送节点数】不能为空")]
        [Range(1, 5, ErrorMessage = "【同时发文件送节点数】必须处于[1,5]区间")]
        private string sendNodeMaxCount;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "【节点同时发送文件数】不能为空")]
        [Range(1, 5, ErrorMessage = "【节点同时发送文件数】必须处于[1,5]区间")]
        private string sameNodeMaxSendFileCount;

        [ObservableProperty]
        [Required(ErrorMessage = "【文件保存路径】不能为空")]
        private string downloadPath = string.Empty;

        [RelayCommand]
        private void ChooseDownloadPath()
        {

        }

        [RelayCommand]
        private void Apply()
        {
            if (ValidateAll())
            {

            }
        }

        [RelayCommand]
        private void Save()
        {
            if (ValidateAll())
            {

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
        }


        private bool ValidateAll()
        {
            ValidateAllProperties();
            return !HasErrors;
        }


    }
}
