using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.Models.Sends
{
    public partial class FileCache : ObservableObject
    {
        [ObservableProperty]
        private string fileName = string.Empty;
        [ObservableProperty]
        private string filePath = string.Empty;
        [ObservableProperty]
        private bool isSelected;
        [ObservableProperty]
        private long fileSize;
        [ObservableProperty]
        private string md5 = string.Empty;
        [ObservableProperty]
        private bool isOpenFromDir;
    }
}
