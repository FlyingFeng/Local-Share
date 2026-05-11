using LocalShare.Desktop.Models.Browsers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LocalShare.Desktop.KeepStates
{
    public class DownloadDataHolder
    {
        private readonly object locker = new object();

        public ObservableCollection<DownloadFileTaskItem> DownloadItems { get; set; } = new ObservableCollection<DownloadFileTaskItem>();


        public void Add(DownloadFileTaskItem item)
        {
            lock (locker)
            {
                var matched = DownloadItems.FirstOrDefault(s => s.NodeName == item.NodeName && s.FileName == item.FileName);
                if (matched == null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DownloadItems.Add(item);
                    });
                }
            }
        }


        public void Remove(string id)
        {
            lock (locker)
            {
                var matched = DownloadItems.FirstOrDefault(s => s.Id == id);
                if (matched != null)
                {
                    DownloadItems.Remove(matched);
                }
            }
        }


    }
}
