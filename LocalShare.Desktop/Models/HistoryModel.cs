using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.Models
{
    public partial class HistoryModel : ObservableObject
    {
        [ObservableProperty]
        private string sendNodeName = string.Empty;
        [ObservableProperty]
        private string receiveNodeName = string.Empty;
        [ObservableProperty]
        private string taskId = string.Empty;
        [ObservableProperty]
        private string fileName = string.Empty;
        [ObservableProperty]
        private string fileFullName = string.Empty;
        [ObservableProperty]
        private int state;
        [ObservableProperty]
        private string sendIpAddress = string.Empty;
        [ObservableProperty]
        private string receiveIpAddress = string.Empty;
        [ObservableProperty]
        private DateTime initTime;
        [ObservableProperty]
        private DateTime lastUpdateTime;
    }
}
