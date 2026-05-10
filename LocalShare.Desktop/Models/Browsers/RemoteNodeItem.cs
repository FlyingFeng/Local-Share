using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        public bool IsLeaf => Children == null || Children.Count == 0;
        public long FileSize { get; set; }
        public ObservableCollection<RemoteNodeItem>? Children { get; set; }



        [RelayCommand]
        private void DownloadFile(object args)
        {
            if (IsLeaf)
            {
                HandyControl.Controls.MessageBox.Show(args.ToString());
            }
        }

    }
}
