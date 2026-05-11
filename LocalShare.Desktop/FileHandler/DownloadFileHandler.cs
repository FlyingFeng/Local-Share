using LocalShare.Desktop.Models.Browsers;
using LocalShare.Desktop.Models.Sends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.FileHandler
{
    public class DownloadFileHandler
    {
        private readonly LocalNode _node;
        public DownloadFileHandler(LocalNode node, DownloadFileTaskItem item)
        {
            _node = node;
            DownloadItem = item;

        }

        public DownloadFileTaskItem? DownloadItem { get; set; }

    }
}
