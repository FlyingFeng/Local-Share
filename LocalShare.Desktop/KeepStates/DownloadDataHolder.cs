using LocalShare.Desktop.Models.Browsers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.KeepStates
{
    public class DownloadDataHolder
    {

        public ObservableCollection<DownloadFileTaskItem> DownloadItems { get; set; } = new ObservableCollection<DownloadFileTaskItem>();

    }
}
