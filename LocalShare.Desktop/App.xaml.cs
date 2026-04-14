using CommonTool;
using HandyControl.Tools;
using Hardcodet.Wpf.TaskbarNotification;
using LocalShare.Desktop.DataContext;
using LocalShare.Desktop.KeepStates;
using LocalShare.Desktop.ViewModels;
using LocalShare.Desktop.Views;
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
        private readonly IHost? _host;
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
                Log.Information("Application started");
                GlobalShared.IpAddress = NetworkHelper.GetLocalIPByUdp();
                GlobalShared.NodeName = ChineseNameGenerator.Generate(NameLength.Two);
                GlobalShared.DownloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "LocalShare");
                if (string.IsNullOrEmpty(GlobalShared.IpAddress))
                {
                    Log.Warning($"OnStartup.GetLocalIpAddress failed.");
                    return;
                }

                _host = Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        services.AddSingleton<MainViewModel>();
                        services.AddSingleton<MainWindow>();
                        services.AddSingleton<UdpDiscoveryService>();
                        services.AddSingleton<HomeDataHolder>();

                        services.AddTransient<SendView>();
                        services.AddTransient<SendViewModel>();
                        services.AddTransient<ReceiveView>();
                        services.AddTransient<ReceiveViewModel>();
                        services.AddTransient<LocalSettingView>();
                        services.AddTransient<LocalSettingViewModel>();
                        services.AddTransient<SendAndReceiveHistoryView>();
                        services.AddTransient<SendAndReceiveHistoryViewModel>();

                        var dbPath = Path.Combine(AppContext.BaseDirectory, "Db", "LocalShare.db");
                        services.AddDbContext<LocalDataContext>(opt =>
                        {
                            opt.UseSqlite($"Data Source={dbPath}");
                        });
                    }).Build();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"Application startup error, {ex.Message}\n{ex.StackTrace}");
                throw;
            }
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
            if (_host != null)
            {
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

                // 禁用休眠
                PreventSleep();
                await _host.StartAsync();
                //var context = _host.Services.GetRequiredService<LocalDataContext>();
                //context.LocalNodes.FirstOrDefault();
                await LoadData();
                var window = _host.Services.GetRequiredService<MainWindow>();
                window.Show();
            }
            else
            {
                Log.Error($"OnStartup failed, host is null");
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
                var discovery = _host.Services.GetRequiredService<UdpDiscoveryService>();
                discovery.Dispose();

                await _host.StopAsync();
                await Log.CloseAndFlushAsync();
            }
        }


        private async Task LoadData()
        {
            var dbContext = _host!.Services.GetRequiredService<LocalDataContext>();
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
                await dbContext.SaveChangesAsync();
            }

            var list = await dbContext.LocalSettings.ToListAsync();

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

            var downloadPathSetting = list.FirstOrDefault(s => s.Key == LocalSettingKey.KeyDownloadPath);
            if (downloadPathSetting == null)
            {
                downloadPathSetting = new DataContext.Entities.LocalSettingEntity
                {
                    Key = LocalSettingKey.KeyDownloadPath,
                    Value = GlobalShared.DownloadPath!
                };
                await dbContext.LocalSettings.AddAsync(downloadPathSetting);
            }
            else
            {
                GlobalShared.DownloadPath = downloadPathSetting.Value;
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
        }


    }

}
