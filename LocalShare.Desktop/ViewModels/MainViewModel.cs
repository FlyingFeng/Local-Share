using CommonTool;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Grpc.Core;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.Models;
using LocalShare.Desktop.Server;
using LocalShare.Desktop.Views;
using LocalShare.Protocol.Define;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LocalShare.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject, IRecipient<MessageModel>
    {
        private readonly UdpDiscoveryService? _udpDiscoveryService = null;
        private readonly IServiceProvider? _services = null;
        private readonly SolidColorBrush selectedBrushColor = new SolidColorBrush(Colors.Orange);
        private readonly SolidColorBrush unSelectedBrushColor = new SolidColorBrush(Colors.White);
        private LocalServer? _localServer;
        private Grpc.Core.Server? _server;
        public MainViewModel() { }

        public MainViewModel(UdpDiscoveryService udpDiscoveryService, IServiceProvider services)
        {
            _services = services;
            _udpDiscoveryService = udpDiscoveryService;
            WeakReferenceMessenger.Default.Register<MessageModel>(this);
            SendViewAction();
        }

        [ObservableProperty]
        private Visibility maskVisibility = Visibility.Collapsed;
        [ObservableProperty]
        private bool isReceive = false;
        [ObservableProperty]
        private bool isSend = false;
        [ObservableProperty]
        private bool isSetting = false;
        [ObservableProperty]
        private bool isHistory = false;
        [ObservableProperty]
        private string nodeName = string.Empty;
        [ObservableProperty]
        private string ipAddress = string.Empty;

        [ObservableProperty]
        private SolidColorBrush? receiveBrush;
        [ObservableProperty]
        private SolidColorBrush? sendBrush;
        [ObservableProperty]
        private SolidColorBrush? settingBrush;
        [ObservableProperty]
        private SolidColorBrush? historyBrush;

        [ObservableProperty]
        private object? mainContent;



        [RelayCommand]
        private void RandomNodeName()
        {
            try
            {
                Random r = new Random(DateTime.UtcNow.Microsecond);
                var i = r.Next() % 2;
                var name = ChineseNameGenerator.Generate((NameLength)i);
                NodeName = name;
            }
            catch (Exception ex)
            {
                Log.Error($"RandomNodeName error, {ex.Message}\n{ex.StackTrace}");
            }
        }


        [RelayCommand]
        private async Task UpdateNode()
        {
            try
            {
                var _dbContext = _services!.GetRequiredService<LocalDataContext>();
                var matched = _dbContext!.LocalNodes.FirstOrDefault();
                if (matched != null)
                {
                    matched.NodeName = NodeName;
                    matched.LastUpdateTime = DateTime.UtcNow;
                    GlobalShared.NodeName = NodeName;
                    _dbContext.Update(matched);
                }
                else
                {
                    matched = new DataContext.Entities.LocalNodeEntity
                    {
                        InitTime = DateTime.UtcNow,
                        LastUpdateTime = DateTime.UtcNow,
                        NodeName = NodeName
                    };
                    await _dbContext!.LocalNodes.AddAsync(matched);
                }
                await _dbContext!.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"SendViewModel.UpdateNode error, {ex.Message}\n{ex.StackTrace}");
            }
        }


        [RelayCommand]
        private void SendViewAction()
        {
            try
            {
                if (IsSend)
                {
                    return;
                }

                IsSend = true;
                IsSetting = false;
                IsReceive = false;
                IsHistory = false;
                SendBrush = selectedBrushColor;
                ReceiveBrush = unSelectedBrushColor;
                SettingBrush = unSelectedBrushColor;
                HistoryBrush = unSelectedBrushColor;

                var view = _services?.GetRequiredService<SendView>();
                MainContent = view;
            }
            catch (Exception ex)
            {
                Log.Error($"SendViewAction error, {ex.Message}\n{ex.StackTrace}");
            }
        }

        [RelayCommand]
        private void ReceiveViewAction()
        {
            try
            {
                if (IsReceive)
                {
                    return;
                }

                IsReceive = true;
                IsSend = false;
                IsSetting = false;
                IsHistory = false;
                ReceiveBrush = selectedBrushColor;
                SendBrush = unSelectedBrushColor;
                SettingBrush = unSelectedBrushColor;
                HistoryBrush = unSelectedBrushColor;

                var view = _services?.GetRequiredService<ReceiveView>();
                MainContent = view;
            }
            catch (Exception ex)
            {
                Log.Error($"ReceiveViewAction error, {ex.Message}\n{ex.StackTrace}");
            }
        }

        [RelayCommand]
        private void HistoryViewAction()
        {
            try
            {
                if (IsHistory)
                {
                    return;
                }
                IsHistory = true;
                IsSend = false;
                IsSetting = false;
                IsReceive = false;
                HistoryBrush = selectedBrushColor;
                SendBrush = unSelectedBrushColor;
                SettingBrush = unSelectedBrushColor;
                ReceiveBrush = unSelectedBrushColor;

                var view = _services?.GetRequiredService<SendAndReceiveHistoryView>();
                MainContent = view;
            }
            catch (Exception ex)
            {
                Log.Error($"HistoryViewAction error, {ex.Message}\n{ex.StackTrace}");
            }
        }

        [RelayCommand]
        private void SettingViewAction()
        {
            try
            {
                if (IsSetting)
                {
                    return;
                }

                IsSetting = true;
                IsReceive = false;
                IsSend = false;
                IsHistory = false;
                SettingBrush = selectedBrushColor;
                SendBrush = unSelectedBrushColor;
                ReceiveBrush = unSelectedBrushColor;
                HistoryBrush = unSelectedBrushColor;

                var view = _services?.GetRequiredService<LocalSettingView>();
                MainContent = view;
            }
            catch (Exception ex)
            {
                Log.Error($"SettingViewAction error, {ex.Message}\n{ex.StackTrace}");

            }
        }

        [RelayCommand]
        private async Task Loaded()
        {
            try
            {
                var dbContext = _services!.GetRequiredService<LocalDataContext>();
                var node = dbContext.LocalNodes.FirstOrDefault();
                if (node != null && !string.IsNullOrEmpty(node.NodeName))
                {
                    GlobalShared.NodeName = node.NodeName;
                }
                else
                {
                    node = new DataContext.Entities.LocalNodeEntity
                    {
                        InitTime = DateTime.UtcNow,
                        LastUpdateTime = DateTime.UtcNow,
                        NodeName = GlobalShared.NodeName!
                    };
                    await dbContext.LocalNodes.AddAsync(node);
                }
                var settings = await dbContext.LocalSettings.ToListAsync();
                var ip = settings.FirstOrDefault(s => s.Key == LocalSettingKey.KeyIpAddress);
                if (ip == null)
                {
                    ip = new DataContext.Entities.LocalSettingEntity
                    {
                        Key = LocalSettingKey.KeyIpAddress,
                        Value = GlobalShared.IpAddress!
                    };
                    await dbContext.LocalSettings.AddAsync(ip);
                }
                else
                {
                    if (ip.Value != GlobalShared.IpAddress)
                    {
                        ip.Value = GlobalShared.IpAddress!;
                    }
                    dbContext.LocalSettings.Update(ip);
                }
                NodeName = GlobalShared.NodeName!;
                IpAddress = GlobalShared.IpAddress!;

                var serverPort = settings.FirstOrDefault(s => s.Key == LocalSettingKey.KeyServerPort);
                if (serverPort == null)
                {
                    serverPort = new DataContext.Entities.LocalSettingEntity
                    {
                        Key = LocalSettingKey.KeyServerPort,
                        Value = GlobalShared.ServerPort.ToString()
                    };
                    await dbContext.LocalSettings.AddAsync(serverPort);
                }
                else
                {
                    GlobalShared.ServerPort = int.Parse(serverPort.Value);
                }
                await dbContext.SaveChangesAsync();
                _localServer = new LocalServer(_services!);
                _server = new Grpc.Core.Server()
                {
                    Services = { LocalShareService.BindService(_localServer) },
                    Ports =
                    {
                        new ServerPort(GlobalShared.IpAddress,GlobalShared.ServerPort,ServerCredentials.Insecure)
                    }
                };
                _server.Start();
                _udpDiscoveryService!.Start();
            }
            catch (Exception ex)
            {
                Log.Error($"MainViewModel.Loaded error, {ex.Message}\n{ex.StackTrace}");
            }

        }

        public void Receive(MessageModel message)
        {
            switch (message.MessageType)
            {
                case MessageType.ShowMask:
                    MaskVisibility = Visibility.Visible;
                    break;
                case MessageType.CloseMask:
                    MaskVisibility = Visibility.Collapsed;
                    break;
            }
        }
    }
}
