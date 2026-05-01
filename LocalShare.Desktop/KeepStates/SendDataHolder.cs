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
    public class SendDataHolder
    {
        //private readonly Timer _timer;
        //public SendDataHolder()
        //{
        //    _timer = new Timer(CheckNodeStatus);
        //    _timer.Change(0, 2000);
        //}


        //private async void CheckNodeStatus(object? state)
        //{
        //    try
        //    {
        //        var utcNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        //        var matched = Nodes.Where(s => Math.Abs(s.LastSeenTime - utcNow) >= 15).ToList();
        //        foreach (var item in matched)
        //        {
        //            await item.CloseAsync();
        //            Application.Current.Dispatcher.Invoke(() =>
        //            {
        //                Nodes.Remove(item);
        //            });
        //        }
        //        //await Task.CompletedTask;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error($"HomeDataHolder.CheckNodeStatus error, {ex.Message}\n{ex.StackTrace}");
        //    }
        //}


        public ObservableCollection<LocalNode> Nodes { get; set; } = [];
        public ObservableCollection<FileCache> FileCaches { get; set; } = [];

        private readonly object locker = new object();


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
