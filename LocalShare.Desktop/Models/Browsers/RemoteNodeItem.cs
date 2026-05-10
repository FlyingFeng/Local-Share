using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.Models.Browsers
{
    public partial class RemoteNodeItem : ObservableObject
    {
        public string NodeName { get; set; } = string.Empty;
        public bool IsLeaft => Children == null || Children.Count == 0;
        public ObservableCollection<RemoteNodeItem>? Children { get; set; }
    }
}
