using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.Models.Sends
{
    public partial class FileTaskModel : ObservableObject
    {
        [ObservableProperty]
        private string fileName = string.Empty;
        [ObservableProperty]
        private long totalSize = 0;
        [ObservableProperty]
        private long currentSize = 0;
        /// <summary>
        /// 0:wait for schedule
        /// 1:sending
        /// 2:stop
        /// 3:finish
        /// 4:error
        /// </summary>
        [ObservableProperty]
        private int state = 0;
        [ObservableProperty]
        private bool isOpenFromDir;
        [ObservableProperty]
        private string md5 = string.Empty;
    }
}
