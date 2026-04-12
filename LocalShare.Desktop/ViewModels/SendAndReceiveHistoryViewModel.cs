using CommunityToolkit.Mvvm.ComponentModel;
using LocalShare.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalShare.Desktop.ViewModels
{
    public partial class SendAndReceiveHistoryViewModel : ObservableObject, IClosable
    {
        private readonly IServiceProvider? _serviceProvider;

        public SendAndReceiveHistoryViewModel()
        {

        }

        public SendAndReceiveHistoryViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        public void Close()
        {
        }

        public ObservableCollection<HistoryModel> SendHistoryRecords { get; set; } = [];
        public ObservableCollection<HistoryModel> ReceiveHistoryRecords { get; set; } = [];

    }
}
