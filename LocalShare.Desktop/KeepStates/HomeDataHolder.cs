using LocalShare.Desktop.Models;
using LocalShare.Desktop.Models.Sends;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LocalShare.Desktop.KeepStates
{
    public class HomeDataHolder
    {
        private readonly Timer _timer;
        public HomeDataHolder()
        {
            _timer = new Timer(CheckNodeStatus);
            _timer.Change(0, 2000);
        }


        private async void CheckNodeStatus(object? state)
        {
            try
            {
                var utcNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var matched = Nodes.Where(s => Math.Abs(s.LastSeenTime - utcNow) >= 10).ToList();
                foreach (var item in matched)
                {
                    await item.CloseAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Nodes.Remove(item);
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error($"HomeDataHolder.CheckNodeStatus error, {ex.Message}\n{ex.StackTrace}");
            }
        }


        public ObservableCollection<LocalNode> Nodes { get; set; } = new();
        public ObservableCollection<FileCache> FileCaches { get; set; } = new();

    }
}
