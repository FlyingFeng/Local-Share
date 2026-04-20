using CommunityToolkit.Mvvm.ComponentModel;
using LocalShare.Desktop.Models.Receives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.ViewModels
{
    public partial class ReceiveViewModel : ObservableObject, IClosable
    {
        public ReceiveViewModel()
        {

        }


        public ObservableCollection<ReceiveFileTaskModel> CacheData { get; set; } = new ObservableCollection<ReceiveFileTaskModel>();


        public void Close()
        {

        }
    }
}
