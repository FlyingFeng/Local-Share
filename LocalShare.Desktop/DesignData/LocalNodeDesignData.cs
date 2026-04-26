using CommunityToolkit.Mvvm.ComponentModel;
using LocalShare.Desktop.Models.Sends;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.DesignData
{
    public class LocalNodeDesignData
    {
        public List<LocalNode> Nodes { get; set; } = [];

        public LocalNodeDesignData()
        {
            Nodes.Add(new LocalNode
            {
                IpAddress = "1.1.1.1",
                IsSelected = false,
                NodeName = "测试1",
                State = 0
            });
            Nodes.Add(new LocalNode
            {
                IpAddress = "1.1.1.2",
                IsSelected = true,
                NodeName = "测试2",
                State = 1
            });
        }


    }
}
