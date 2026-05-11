using LocalShare.Desktop.Models.Browsers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.DesignData
{
    public class BrowserDesignData
    {
        public ObservableCollection<RemoteNodeItem> Nodes { get; set; } = [];

        public ObservableCollection<DownloadFileTaskItem> DownloadFileTasks { get; set; } = [];
        public BrowserDesignData()
        {
            DownloadFileTasks.Add(new DownloadFileTaskItem
            {
                CurrentSize = 66666,
                FileName = "1.exe",
                NodeName = "test",
                TotalSize = 99999
            });
            DownloadFileTasks.Add(new DownloadFileTaskItem
            {
                TotalSize = 1234567,
                CurrentSize = 12345,
                FileName = "3.pdf",
                NodeName = "test1"
            });

            Nodes.Add(new RemoteNodeItem
            {
                NodeName = "根节点1"
            });
            Nodes.Add(new RemoteNodeItem()
            {
                NodeName = "根节点2"
            });
            Nodes.Add(new RemoteNodeItem
            {
                NodeName = "根节点3",
                Children = new ObservableCollection<RemoteNodeItem>
                  {
                       new RemoteNodeItem
                       {
                            NodeName="子节点1",
                            FileSize=999999
                       }
                  }
            });
        }

    }
}
