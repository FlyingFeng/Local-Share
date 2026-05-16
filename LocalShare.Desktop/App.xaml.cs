using CommonTool;
using HandyControl.Tools;
using Hardcodet.Wpf.TaskbarNotification;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.KeepStates;
using LocalShare.Desktop.ViewModels;
using LocalShare.Desktop.Views;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace LocalShare.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        private TaskbarIcon? _trayIcon;

        // ── 阻止休眠 API ─────────────────────────────────────────────

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);

        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;

        private void PreventSleep()
        {
            // ES_CONTINUOUS       = 保持该状态直到再次调用
            // ES_SYSTEM_REQUIRED  = 阻止系统休眠
            // ES_DISPLAY_REQUIRED = 阻止屏幕关闭（按需加）
            SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
            Log.Information("已禁用系统休眠");
        }

        private void RestoreSleep()
        {
            // 只传 ES_CONTINUOUS 表示恢复系统默认电源策略
            SetThreadExecutionState(ES_CONTINUOUS);
            Log.Information("已恢复系统休眠");
        }


        public App()
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Async(s => s.File("logs/app-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 31,
                fileSizeLimitBytes: 10485760,
                rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] - {Message:lj}{NewLine}{Exception}"))
                .CreateLogger();
            try
            {
                DispatcherUnhandledException += App_DispatcherUnhandledException;

                GlobalShared.IpAddress = NetworkHelper.GetLocalIPByUdp();
                if (string.IsNullOrEmpty(GlobalShared.IpAddress))
                {
                    Log.Warning($"OnStartup.GetLocalIpAddress failed.");
                    return;
                }
                GlobalShared.NodeName = ChineseNameGenerator.Generate(NameLength.Two);
                GlobalShared.SaveFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "LocalShare");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"Application startup error, {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Log.Error(e.Exception?.Message ?? "no message");
            Log.Error(e.Exception?.StackTrace ?? "no stackTrace");
        }

        private void ExitApp()
        {
            _trayIcon?.Dispose();
            Shutdown();
        }

        private void ShowMainWindow()
        {
            if (MainWindow != null)
            {
                MainWindow.Show();
                MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                Log.Information("Application started");
                // 禁用休眠
                PreventSleep();
                _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
                var contextMenu = new System.Windows.Controls.ContextMenu();

                var showItem = new System.Windows.Controls.MenuItem { Header = "恢复主窗口" };
                showItem.Click += (s, args) => ShowMainWindow();

                var exitItem = new System.Windows.Controls.MenuItem { Header = "关闭程序" };
                exitItem.Click += (s, args) => ExitApp();

                contextMenu.Items.Add(showItem);
                contextMenu.Items.Add(new System.Windows.Controls.Separator());
                contextMenu.Items.Add(exitItem);
                _trayIcon.ContextMenu = contextMenu;
                _trayIcon.TrayMouseDoubleClick += (s, args) => ShowMainWindow();
                await LoadData();
                GlobalShared.HttpPort = GlobalShared.ServerPort + 10;
                _host = Host.CreateDefaultBuilder()
                 .ConfigureWebHostDefaults(webBuilder =>
                 {
                     webBuilder.UseUrls($"http://0.0.0.0:{GlobalShared.HttpPort}");
                     webBuilder.Configure((app) =>
                     {
                         app.UseRouting();
                         app.UseEndpoints(endpoints =>
                         {
                             endpoints.MapControllers();
                         });
                     });
                 })
                 .ConfigureServices((context, services) =>
                 {
                     services.AddControllers();
                     services.AddSingleton<MainViewModel>();
                     services.AddSingleton<MainWindow>();
                     services.AddSingleton<UdpMulticastDiscoveryService>();
                     services.AddSingleton<SendDataHolder>();
                     services.AddSingleton<ReceiveDataHolder>();
                     services.AddSingleton<DownloadDataHolder>();
                     services.AddSingleton<RpcChannelHolder>();

                     services.AddTransient<SendView>();
                     services.AddTransient<SendViewModel>();
                     services.AddTransient<ReceiveView>();
                     services.AddTransient<ReceiveViewModel>();
                     services.AddTransient<LocalSettingView>();
                     services.AddTransient<LocalSettingViewModel>();
                     services.AddTransient<SendAndReceiveHistoryView>();
                     services.AddTransient<SendAndReceiveHistoryViewModel>();
                     services.AddTransient<BrowserView>();
                     services.AddTransient<BrowserViewModel>();
                     services.AddTransient<AboutView>();

                 }).Build();

                await _host.StartAsync();
                Log.Information($"Listen at 0.0.0.0:{GlobalShared.HttpPort}");
                var window = _host.Services.GetRequiredService<MainWindow>();
                window.Show();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                HandyControl.Controls.MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            Log.Information("Application stopped");
            // 恢复休眠
            RestoreSleep();
            _trayIcon?.Dispose();
            if (_host != null)
            {
                // 停止 UDP 发现服务
                var discovery = _host.Services.GetRequiredService<UdpMulticastDiscoveryService>();
                discovery.Dispose();

                await _host.StopAsync();
                await Log.CloseAndFlushAsync();
            }
        }


        private async Task LoadData()
        {
            using var dbContext = new LocalDataContext();
            var localNode = await dbContext.LocalNodes.FirstOrDefaultAsync();
            if (localNode != null)
            {
                GlobalShared.NodeName = localNode.NodeName;
            }
            else
            {
                localNode = new DataContext.Entities.LocalNodeEntity
                {
                    InitTime = DateTime.UtcNow,
                    LastUpdateTime = DateTime.UtcNow,
                    NodeName = GlobalShared.NodeName!
                };
                await dbContext.LocalNodes.AddAsync(localNode);
                //await dbContext.SaveChangesAsync();
            }

            var list = await dbContext.LocalSettings.ToListAsync();

            var downloadSpeed = list.FirstOrDefault(s => s.Key == LocalSettingKey.KeyDownloadSpeed);
            if (downloadSpeed == null)
            {
                downloadSpeed = new DataContext.Entities.LocalSettingEntity
                {
                    Key = LocalSettingKey.KeyDownloadSpeed,
                    Value = GlobalShared.DownloadSpeed.ToString()
                };
                await dbContext.LocalSettings.AddAsync(downloadSpeed);
            }
            else
            {
                GlobalShared.DownloadSpeed = int.Parse(downloadSpeed.Value);
            }

            var transferSpeed = list.FirstOrDefault(s => s.Key == LocalSettingKey.KeyTransferSpeed);
            if (transferSpeed == null)
            {
                transferSpeed = new DataContext.Entities.LocalSettingEntity
                {
                    Key = LocalSettingKey.KeyTransferSpeed,
                    Value = GlobalShared.TransferSpeed.ToString()
                };
                await dbContext.LocalSettings.AddAsync(transferSpeed);
            }
            else
            {
                GlobalShared.TransferSpeed = int.Parse(transferSpeed.Value);
            }

            var multicastAddressSetting = list.FirstOrDefault(s => s.Key == LocalSettingKey.KeyMulticastAddress);
            if (multicastAddressSetting == null)
            {
                multicastAddressSetting = new DataContext.Entities.LocalSettingEntity
                {
                    Key = LocalSettingKey.KeyMulticastAddress,
                    Value = GlobalShared.MulticastAddress
                };
                await dbContext.LocalSettings.AddAsync(multicastAddressSetting);
            }
            else
            {
                GlobalShared.MulticastAddress = multicastAddressSetting.Value;
            }

            var brocastPortSetting = list.FirstOrDefault(s => s.Key == LocalSettingKey.KeyBrocastPort);
            if (brocastPortSetting == null)
            {
                brocastPortSetting = new DataContext.Entities.LocalSettingEntity()
                {
                    Key = LocalSettingKey.KeyBrocastPort,
                    Value = GlobalShared.BroadcastPort.ToString()
                };
                await dbContext.LocalSettings.AddAsync(brocastPortSetting);
            }
            else
            {
                GlobalShared.BroadcastPort = int.Parse(brocastPortSetting.Value);
            }

            var serverPortSetting = list.FirstOrDefault(s => s.Key == LocalSettingKey.KeyServerPort);
            if (serverPortSetting == null)
            {
                serverPortSetting = new DataContext.Entities.LocalSettingEntity
                {
                    Key = LocalSettingKey.KeyServerPort,
                    Value = GlobalShared.ServerPort.ToString()
                };
                await dbContext.LocalSettings.AddAsync(serverPortSetting);
            }
            else
            {
                GlobalShared.ServerPort = int.Parse(serverPortSetting.Value);
            }

            var saveFilePathSetting = list.FirstOrDefault(s => s.Key == LocalSettingKey.KeySaveFilePath);
            if (saveFilePathSetting == null)
            {
                saveFilePathSetting = new DataContext.Entities.LocalSettingEntity
                {
                    Key = LocalSettingKey.KeySaveFilePath,
                    Value = GlobalShared.SaveFilePath!
                };
                await dbContext.LocalSettings.AddAsync(saveFilePathSetting);
            }
            else
            {
                GlobalShared.SaveFilePath = saveFilePathSetting.Value;
            }

            var sameNodeMaxSendFileCountSetting = list.FirstOrDefault(s => s.Key == LocalSettingKey.KeySameNodeMaxSendFileCount);
            if (sameNodeMaxSendFileCountSetting == null)
            {
                sameNodeMaxSendFileCountSetting = new DataContext.Entities.LocalSettingEntity
                {
                    Key = LocalSettingKey.KeySameNodeMaxSendFileCount,
                    Value = GlobalShared.SameNodeMaxSendFileCount.ToString()
                };
                await dbContext.LocalSettings.AddAsync(sameNodeMaxSendFileCountSetting);
            }
            else
            {
                GlobalShared.SameNodeMaxSendFileCount = int.Parse(sameNodeMaxSendFileCountSetting.Value);
            }

            var sendNodeMaxCountSetting = list.FirstOrDefault(s => s.Key == LocalSettingKey.KeySendNodeMaxCount);
            if (sendNodeMaxCountSetting == null)
            {
                sendNodeMaxCountSetting = new DataContext.Entities.LocalSettingEntity
                {
                    Key = LocalSettingKey.KeySendNodeMaxCount,
                    Value = GlobalShared.SendNodeMaxCount.ToString()
                };
                await dbContext.LocalSettings.AddAsync(sendNodeMaxCountSetting);
            }
            else
            {
                GlobalShared.SendNodeMaxCount = int.Parse(sendNodeMaxCountSetting.Value);
            }

            if (!Directory.Exists(GlobalShared.SaveFilePath))
            {
                Directory.CreateDirectory(GlobalShared.SaveFilePath!);
            }
            await dbContext.SaveChangesAsync();
        }


    }

}
