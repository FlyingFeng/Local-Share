using LocalShare.Desktop.Models.Sends;

namespace LocalShare.Desktop.DesignData
{
    public class LocalFileCacheDesignData
    {
        public List<FileCache> FileCaches { get; set; } = [];

        public LocalFileCacheDesignData()
        {
            FileCaches.Add(new FileCache
            {
                FileName = "12.txt",
                FilePath = "D:\\1\\2\\12.txt",
                FileSize = 55441122,
                IsOpenFromDir = true,
                Md5 = "151515",
                IsSelected = true
            });
            FileCaches.Add(new FileCache
            {
                IsSelected = false,
                Md5 = "12324343",
                IsOpenFromDir = false,
                FileSize = 456131342351,
                FileName = "test.txt",
                FilePath = "D:\\1\\3\\test.txt"
            });
        }
    }
}
