using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.Models.Browsers
{
    public partial class DownloadFileTaskItem : ObservableObject
    {

        public string Id { get; set; } = string.Empty;

        [ObservableProperty]
        private string nodeName = string.Empty;
        [ObservableProperty]
        private string fileName = string.Empty;
        [ObservableProperty]
        private long totalSize = 0;
        [ObservableProperty]
        private long currentSize = 0;
        [ObservableProperty]
        private int progress = 0;
        /// <summary>
        /// 0:wait for schedule
        /// 1:sending/receiving/downloading
        /// 2:stop
        /// 3:finish
        /// 4:error
        /// </summary>
        [ObservableProperty]
        private int state;

        partial void OnCurrentSizeChanged(long value)
        {
            if (TotalSize > 0 && value > 0)
            {
                Progress = (int)((CurrentSize * 1.0) / TotalSize * 100);
            }
        }

    }
}
