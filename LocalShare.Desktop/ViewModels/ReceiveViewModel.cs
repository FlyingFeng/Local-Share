using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalShare.Desktop.KeepStates;
using LocalShare.Desktop.Models.Receives;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LocalShare.Desktop.ViewModels
{
    public partial class ReceiveViewModel : ObservableObject, IClosable
    {
        public ReceiveViewModel()
        {

        }
        private readonly ReceiveDataHolder? _receiveDataHolder;

        public ReceiveViewModel(ReceiveDataHolder? receiveDataHolder)
        {
            _receiveDataHolder = receiveDataHolder;
            _receiveDataHolder!.OnReceiveFileTaskAdded += ReceiveDataHolder_OnReceiveFileTaskAdded;
            _receiveDataHolder!.OnReceiveFileTaskRemoved += ReceiveDataHolder_OnReceiveFileTaskRemoved;
        }

        private void ReceiveDataHolder_OnReceiveFileTaskRemoved(FileHandler.ReceiveFileHandler handler)
        {
            try
            {
                if (handler != null && handler.TaskModel != null)
                {
                    var matched = CacheData.FirstOrDefault(s => s.TaskId == handler.TaskModel.TaskId);
                    if (matched != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CacheData.Remove(matched);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"ReceiveDataHolder_OnReceiveFileTaskRemoved error, {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ReceiveDataHolder_OnReceiveFileTaskAdded(FileHandler.ReceiveFileHandler handler)
        {
            try
            {
                if (handler != null && handler.TaskModel != null)
                {
                    var matched = CacheData.FirstOrDefault(s => s.TaskId == handler.TaskModel.TaskId);
                    if (matched == null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CacheData.Add(handler.TaskModel);
                        });
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Error($"ReceiveDataHolder_OnReceiveFileTaskAdded error, {ex.Message}\n{ex.StackTrace}");
            }

        }

        public ObservableCollection<ReceiveFileTaskModel> CacheData { get; set; } = new ObservableCollection<ReceiveFileTaskModel>();

        [RelayCommand]
        private void Loaded()
        {
            var allHandlers = _receiveDataHolder!.GetAllHandlers();
            CacheData.Clear();
            foreach (var item in allHandlers)
            {
                if (item.TaskModel != null)
                {
                    CacheData.Add(item.TaskModel);
                }
            }

        }

        [RelayCommand]
        private void RemoveFileTask(object args)
        {
            if (args != null && args is string taskId)
            {
                var matched = CacheData.FirstOrDefault(s => s.TaskId == taskId);
                if (matched != null &&
                    (matched.State == 3 ||
                    matched.State == 4))
                {
                    CacheData.Remove(matched);
                    _receiveDataHolder!.RemoveReceiveFileHandler(taskId);
                }
            }
        }

        public void Close()
        {
            _receiveDataHolder!.OnReceiveFileTaskAdded -= ReceiveDataHolder_OnReceiveFileTaskAdded;
            _receiveDataHolder!.OnReceiveFileTaskRemoved -= ReceiveDataHolder_OnReceiveFileTaskRemoved;
        }
    }
}
