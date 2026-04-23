using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        [ObservableProperty]
        private string fullFileName = string.Empty;
        [ObservableProperty]
        private int progress = 0;
        [ObservableProperty]
        private string taskId = string.Empty;

        partial void OnCurrentSizeChanged(long value)
        {
            if (TotalSize > 0 && value > 0)
            {
                Progress = (int)((CurrentSize * 1.0) / TotalSize * 100);
            }
        }

    }
}
