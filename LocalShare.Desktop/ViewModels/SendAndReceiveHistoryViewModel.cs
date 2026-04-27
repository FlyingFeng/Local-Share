using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.Models;
using Microsoft.EntityFrameworkCore;
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
    public partial class SendAndReceiveHistoryViewModel : ObservableObject, IClosable
    {

        public SendAndReceiveHistoryViewModel()
        {

        }


        [ObservableProperty]
        private string searchTaskId = string.Empty;
        [ObservableProperty]
        private string searchFileName = string.Empty;
        [ObservableProperty]
        private string searchSendNodeName = string.Empty;
        [ObservableProperty]
        private string searchReceiveNodeName = string.Empty;

        private int selectedTabIndex = 0;

        [RelayCommand]
        private void TabSelectionChanged(object args)
        {
            if (args is int index)
            {
                selectedTabIndex = index;
            }
        }

        [RelayCommand]
        private async Task Clear()
        {
            try
            {
                using var db = new LocalDataContext();
                var sendList = await db.SendFileTasks.ToListAsync();
                var receiveList = await db.ReceiveFileTasks.ToListAsync();

                db.SendFileTasks.RemoveRange(sendList);
                db.ReceiveFileTasks.RemoveRange(receiveList);
                await db.SaveChangesAsync();
                SendHistoryRecords.Clear();
                ReceiveHistoryRecords.Clear();
            }
            catch (Exception ex)
            {
                Log.Error($"SendAndReceiveHistoryViewModel.Clear error, {ex.Message}\n{ex.StackTrace}");
            }

        }

        [RelayCommand]
        private async Task Search()
        {
            try
            {
                using var db = new LocalDataContext();
                if (selectedTabIndex == 0) //send
                {
                    var query = db.SendFileTasks.AsNoTracking().AsQueryable();
                    if (!string.IsNullOrWhiteSpace(SearchTaskId))
                    {
                        query = query.Where(s => s.TaskId.Contains(SearchTaskId));
                    }
                    if (!string.IsNullOrWhiteSpace(SearchFileName))
                    {
                        query = query.Where(s => s.FileName.Contains(SearchFileName));
                    }
                    if (!string.IsNullOrWhiteSpace(SearchSendNodeName))
                    {
                        query = query.Where(s => s.SendNodeName.Contains(SearchSendNodeName));
                    }
                    if (!string.IsNullOrWhiteSpace(SearchReceiveNodeName))
                    {
                        query = query.Where(s => s.ReceiveNodeName.Contains(SearchReceiveNodeName));
                    }
                    var list = await query.OrderByDescending(s => s.InitTime).ToListAsync();
                    SendHistoryRecords.Clear();
                    foreach (var item in list)
                    {
                        SendHistoryRecords.Add(new HistoryModel
                        {
                            FileFullName = item.FileFullName,
                            FileName = item.FileName,
                            InitTime = item.InitTime.ToLocalTime(),
                            LastUpdateTime = item.LastUpdateTime.ToLocalTime(),
                            ReceiveIpAddress = item.ReceiveIpAddress,
                            ReceiveNodeName = item.ReceiveNodeName,
                            SendIpAddress = item.SendIpAddress,
                            SendNodeName = item.SendNodeName,
                            State = item.State,
                            TaskId = item.TaskId
                        });
                    }
                }
                else if (selectedTabIndex == 1) //receive
                {
                    var query = db.ReceiveFileTasks.AsNoTracking().AsQueryable();
                    if (!string.IsNullOrWhiteSpace(SearchTaskId))
                    {
                        query = query.Where(s => s.TaskId.Contains(SearchTaskId));
                    }
                    if (!string.IsNullOrWhiteSpace(SearchFileName))
                    {
                        query = query.Where(s => s.FileName.Contains(SearchFileName));
                    }
                    if (!string.IsNullOrWhiteSpace(SearchSendNodeName))
                    {
                        query = query.Where(s => s.SendNodeName.Contains(SearchSendNodeName));
                    }
                    if (!string.IsNullOrWhiteSpace(SearchReceiveNodeName))
                    {
                        query = query.Where(s => s.ReceiveNodeName.Contains(SearchReceiveNodeName));
                    }
                    var list = await query.OrderByDescending(s => s.InitTime).ToListAsync();
                    ReceiveHistoryRecords.Clear();
                    foreach (var item in list)
                    {
                        ReceiveHistoryRecords.Add(new HistoryModel
                        {
                            FileFullName = item.FileFullName,
                            FileName = item.FileName,
                            InitTime = item.InitTime.ToLocalTime(),
                            LastUpdateTime = item.LastUpdateTime.ToLocalTime(),
                            ReceiveIpAddress = item.ReceiveIpAddress,
                            ReceiveNodeName = item.ReceiveNodeName,
                            SendIpAddress = item.SendIpAddress,
                            SendNodeName = item.SendNodeName,
                            State = item.State,
                            TaskId = item.TaskId
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"SendAndReceiveHistoryViewModel.Search error, {ex.Message}\n{ex.StackTrace}");
            }
        }

        [RelayCommand]
        private async Task Loaded()
        {
            using var dbContext = new LocalDataContext();
            var sendData = await dbContext.SendFileTasks.Where(s => s.State == 3 || s.State == 4).ToListAsync();
            var receiveData = await dbContext.ReceiveFileTasks.Where(s => s.State == 3 || s.State == 4).ToListAsync();

            SendHistoryRecords.Clear();
            foreach (var send in sendData)
            {
                SendHistoryRecords.Add(new HistoryModel
                {
                    FileFullName = send.FileFullName,
                    FileName = send.FileName,
                    InitTime = send.InitTime.ToLocalTime(),
                    LastUpdateTime = send.LastUpdateTime.ToLocalTime(),
                    ReceiveIpAddress = send.ReceiveIpAddress,
                    ReceiveNodeName = send.ReceiveNodeName,
                    SendIpAddress = send.SendIpAddress,
                    SendNodeName = send.SendNodeName,
                    State = send.State,
                    TaskId = send.TaskId
                });
            }

            ReceiveHistoryRecords.Clear();
            foreach (var receive in receiveData)
            {
                ReceiveHistoryRecords.Add(new HistoryModel
                {
                    FileFullName = receive.FileFullName,
                    FileName = receive.FileName,
                    InitTime = receive.InitTime.ToLocalTime(),
                    LastUpdateTime = receive.LastUpdateTime.ToLocalTime(),
                    ReceiveIpAddress = receive.ReceiveIpAddress,
                    ReceiveNodeName = receive.ReceiveNodeName,
                    SendIpAddress = receive.SendIpAddress,
                    SendNodeName = receive.SendNodeName,
                    State = receive.State,
                    TaskId = receive.TaskId
                });
            }
        }


        public void Close()
        {
        }

        public ObservableCollection<HistoryModel> SendHistoryRecords { get; set; } = [];
        public ObservableCollection<HistoryModel> ReceiveHistoryRecords { get; set; } = [];

    }
}
