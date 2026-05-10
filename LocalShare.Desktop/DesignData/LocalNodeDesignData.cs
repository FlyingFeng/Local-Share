using LocalShare.Desktop.Models.Sends;

namespace LocalShare.Desktop.DesignData
{
    public class LocalNodeDesignData
    {
        public List<LocalNode> Nodes { get; set; } = [];

        public LocalNodeDesignData()
        {
            Nodes.Add(new LocalNode(null)
            {
                IpAddress = "1.1.1.1",
                IsSelected = false,
                NodeName = "测试1",
                State = 0
            });
            Nodes.Add(new LocalNode(null)
            {
                IpAddress = "1.1.1.2",
                IsSelected = true,
                NodeName = "测试2",
                State = 1
            });
        }


    }
}
