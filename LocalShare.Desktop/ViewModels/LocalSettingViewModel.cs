using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.ViewModels
{
    public partial class LocalSettingViewModel : ObservableObject, IClosable
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
        private string serverPort = string.Empty;
        [ObservableProperty]
        private string brocastPort = string.Empty;
        [ObservableProperty]
        private int sendNodeMaxCount;
        [ObservableProperty]
        private int sameNodeMaxSendFileCount;
        [ObservableProperty]
        private string downloadPath = string.Empty;

        [RelayCommand]
        private void ChooseDownloadPath()
        {

        }

        [RelayCommand]
        private void Apply()
        {

        }

        [RelayCommand]
        private void Save()
        {

        }


    }
}
