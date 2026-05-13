using LocalShare.Desktop.Models.Sends;
using System.Collections.ObjectModel;
using System.Windows;

namespace LocalShare.Desktop.KeepStates
{
    public class SendDataHolder
    {
        public ObservableCollection<LocalNode> Nodes { get; set; } = [];
        public ObservableCollection<FileCache> FileCaches { get; set; } = [];

        private readonly object locker = new object();


        public List<FileCache> GetFiles(bool isSelected)
        {
            lock (locker)
            {
                var result = new List<FileCache>();
                var query = FileCaches.AsQueryable();
                query = query.Where(s => s.IsSelected == isSelected);
                result.AddRange(query.ToList());
                return result;
            }
        }

        public FileCache? GetFile(string fileName)
        {
            return FileCaches.FirstOrDefault(s => s.FileName == fileName);
        }


        public void AddFile(FileCache file)
        {
            lock (locker)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FileCaches.Add(file);
                });
            }
        }


        public List<FileCache> GetAllFiles()
        {
            lock (locker)
            {
                var result = new List<FileCache>();
                foreach (var item in FileCaches)
                {
                    result.Add(item);
                }
                return result;
            }
        }

        public LocalNode? GetNode(string ipAddress = "", int port = 0, string nodeName = "")
        {
            lock (locker)
            {
                LocalNode? node = null;
                var query = Nodes.AsQueryable();
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    query = query.Where(s => s.IpAddress == ipAddress);
                }
                if (port > 0)
                {
                    query = query.Where(s => s.Port == port);
                }
                if (!string.IsNullOrEmpty(nodeName))
                {
                    query = query.Where(s => s.NodeName == nodeName);
                }

                node = query.FirstOrDefault();
                return node;
            }
        }


        public List<LocalNode> GetAllNodes()
        {
            lock (locker)
            {
                var result = new List<LocalNode>();
                foreach (var item in Nodes)
                {
                    result.Add(item);
                }
                return result;
            }
        }

        public void AddNode(LocalNode node)
        {
            lock (locker)
            {
                var matched = Nodes.FirstOrDefault(s => s.IpAddress == node.IpAddress && s.Port == node.Port);
                if (matched == null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Nodes.Add(node);
                    });
                }
            }
        }


    }
}
