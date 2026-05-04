
namespace LocalShare.Desktop
{
    internal static class DownloadLinkHolder
    {
        private static readonly Dictionary<string, string> links = new Dictionary<string, string>();
        private static readonly object locker = new object();


        public static void AddDownloadLink(string id, string filePath)
        {
            lock (locker)
            {
                links[id] = filePath;
            }
        }

        public static string GetDownloadPath(string id)
        {
            lock (locker)
            {
                if (links.TryGetValue(id, out string? value)) return value;
            }
            return string.Empty;
        }



    }
}
