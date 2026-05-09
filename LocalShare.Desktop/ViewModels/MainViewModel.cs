using CommonTool;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Grpc.Core;
using HandyControl.Controls;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.KeepStates;
using LocalShare.Desktop.Models;
using LocalShare.Desktop.Server;
using LocalShare.Desktop.Views;
using LocalShare.Protocol.Define;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Windows;
using System.Windows.Media;
using MessageBox = HandyControl.Controls.MessageBox;

namespace LocalShare.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject, IRecipient<MessageModel>
    {
        private readonly UdpMulticastDiscoveryService? _udpDiscoveryService = null;
        private readonly ReceiveDataHolder? _receiveDataHolder;
        private readonly SendDataHolder? _sendDataHolder;
        private readonly IServiceProvider? _services = null;
        private readonly SolidColorBrush selectedBrushColor = new SolidColorBrush(Colors.Orange);
        private readonly SolidColorBrush unSelectedBrushColor = new SolidColorBrush(Colors.White);
        private LocalServer? _localServer;
        private Grpc.Core.Server? _server;
        private bool loaded = false;
        public MainViewModel() { }

        public MainViewModel(UdpMulticastDiscoveryService udpDiscoveryService,
            IServiceProvider services,
            SendDataHolder sendDataHolder,
            ReceiveDataHolder receiveDataHolder)
        {
            _services = services;
            _udpDiscoveryService = udpDiscoveryService;
            _sendDataHolder = sendDataHolder;
            _receiveDataHolder = receiveDataHolder;
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
        private bool isAbout = false;
        [ObservableProperty]
        private bool isBrowser = false;
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
        private SolidColorBrush? aboutBrush;
        [ObservableProperty]
        private SolidColorBrush? browserBrush;

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
                MessageBox.Show($"产生随机名称失败\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        [RelayCommand]
        private async Task UpdateNode()
        {
            try
            {
                using var _dbContext = new LocalDataContext();
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
                MessageBox.Show($"保存失败\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);

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
                IsAbout = false;
                IsBrowser = false;
                SendBrush = selectedBrushColor;
                ReceiveBrush = unSelectedBrushColor;
                SettingBrush = unSelectedBrushColor;
                HistoryBrush = unSelectedBrushColor;
                AboutBrush = unSelectedBrushColor;
                BrowserBrush = unSelectedBrushColor;

                var view = _services!.CreateScope().ServiceProvider.GetRequiredService<SendView>();
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
                IsAbout = false;
                IsBrowser = false;
                ReceiveBrush = selectedBrushColor;
                SendBrush = unSelectedBrushColor;
                SettingBrush = unSelectedBrushColor;
                HistoryBrush = unSelectedBrushColor;
                AboutBrush = unSelectedBrushColor;
                BrowserBrush = unSelectedBrushColor;

                var view = _services!.CreateScope().ServiceProvider.GetRequiredService<ReceiveView>();
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
                IsAbout = false;
                IsBrowser = false;
                HistoryBrush = selectedBrushColor;
                SendBrush = unSelectedBrushColor;
                SettingBrush = unSelectedBrushColor;
                ReceiveBrush = unSelectedBrushColor;
                AboutBrush = unSelectedBrushColor;
                BrowserBrush = unSelectedBrushColor;

                var view = _services!.CreateScope().ServiceProvider.GetRequiredService<SendAndReceiveHistoryView>();
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
                IsAbout = false;
                IsBrowser = false;
                SettingBrush = selectedBrushColor;
                SendBrush = unSelectedBrushColor;
                ReceiveBrush = unSelectedBrushColor;
                HistoryBrush = unSelectedBrushColor;
                AboutBrush = unSelectedBrushColor;
                BrowserBrush = unSelectedBrushColor;

                var view = _services!.CreateScope().ServiceProvider.GetRequiredService<LocalSettingView>();
                MainContent = view;
            }
            catch (Exception ex)
            {
                Log.Error($"SettingViewAction error, {ex.Message}\n{ex.StackTrace}");

            }
        }
        [RelayCommand]
        private void AboutViewAction()
        {
            try
            {
                if (IsAbout)
                {
                    return;
                }

                IsAbout = true;
                IsReceive = false;
                IsSend = false;
                IsHistory = false;
                IsSetting = false;
                IsBrowser = false;
                AboutBrush = selectedBrushColor;
                SettingBrush = unSelectedBrushColor;
                SendBrush = unSelectedBrushColor;
                ReceiveBrush = unSelectedBrushColor;
                HistoryBrush = unSelectedBrushColor;
                BrowserBrush = unSelectedBrushColor;

                var view = _services!.CreateScope().ServiceProvider.GetRequiredService<AboutView>();
                MainContent = view;
            }
            catch (Exception ex)
            {
                Log.Error($"AboutViewAction error, {ex.Message}\n{ex.StackTrace}");
            }
        }

        [RelayCommand]
        private void BrowserViewAction()
        {
            try
            {
                if (IsAbout)
                {
                    return;
                }

                IsBrowser = true;
                IsAbout = false;
                IsReceive = false;
                IsSend = false;
                IsHistory = false;
                IsSetting = false;
                BrowserBrush = selectedBrushColor;
                SettingBrush = unSelectedBrushColor;
                SendBrush = unSelectedBrushColor;
                ReceiveBrush = unSelectedBrushColor;
                HistoryBrush = unSelectedBrushColor;
                AboutBrush = unSelectedBrushColor;

                var view = _services!.CreateScope().ServiceProvider.GetRequiredService<BrowserView>();
                MainContent = view;
            }
            catch (Exception ex)
            {
                Log.Error($"AboutViewAction error, {ex.Message}\n{ex.StackTrace}");
            }
        }


        [RelayCommand]
        private async Task Loaded()
        {
            if (loaded)
            {
                return;
            }
            try
            {
                loaded = true;
                NodeName = GlobalShared.NodeName!;
                IpAddress = GlobalShared.IpAddress!;
                _localServer = new LocalServer(_receiveDataHolder!, _sendDataHolder!);
                await StartServer();
                StartMulticast();
                loaded = true;
                //StartBrocast();
            }
            catch (Exception ex)
            {
                loaded = false;
                HandyControl.Controls.MessageBox.Show("加载失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"MainViewModel.Loaded error, {ex.Message}\n{ex.StackTrace}");
            }
        }

        public async void Receive(MessageModel message)
        {
            switch (message.MessageType)
            {
                case MessageType.ShowMask:
                    MaskVisibility = Visibility.Visible;
                    break;
                case MessageType.CloseMask:
                    MaskVisibility = Visibility.Collapsed;
                    break;
                case MessageType.RestartBrocast:
                    StartBrocast();
                    break;
                case MessageType.RestartServer:
                    await StartServer();
                    break;
                case MessageType.RestartMulticast:
                    StartMulticast();
                    break;
            }
        }

        private async Task StartServer()
        {
            try
            {
                if (_server != null)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await _server.KillAsync().WaitAsync(cts.Token);
                    //await _server.KillAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"StartServer.kill error, {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _server = null;
            }

            _server = new Grpc.Core.Server()
            {
                Services = { LocalShareService.BindService(_localServer) },
                Ports =
                    {
                        new ServerPort(GlobalShared.IpAddress,GlobalShared.ServerPort,ServerCredentials.Insecure)
                    }
            };
            try
            {
                _server.Start();
                Growl.Info("服务启动成功");
            }
            catch (Exception ex)
            {
                _server = null;
                MessageBox.Show("启动服务失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error($"StartServer.Start error, {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void StartMulticast()
        {
            _udpDiscoveryService!.Start();
            Growl.Info("启动组播成功");
        }

        private void StartBrocast()
        {
            //_udpDiscoveryService!.Start();
            //Growl.Info("启动广播成功");
        }


    }
}
