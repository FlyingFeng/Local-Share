using LocalShare.Desktop.Models.Receives;

namespace LocalShare.Desktop.DesignData
{
    public class ReceiveFileTaskDesignData
    {
        public List<ReceiveFileTaskModel> CacheData { get; set; } = [];

        public ReceiveFileTaskDesignData()
        {
            CacheData.Add(new ReceiveFileTaskModel
            {
                CurrentSize = 755,
                FileName = "1.txt",
                Progress = 50,
                SaveFilePath = "D:\\1\\2\\3\\1.txt",
                SendNodeName = "测试节点",
                State = 1,
                TaskId = Guid.NewGuid().ToString(),
                TotalSize = 150025

            });
        }
    }
}
