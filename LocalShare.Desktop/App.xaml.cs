using CommonTool;
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
using System.Windows;

namespace LocalShare.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost? _host;


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

        protected override async void OnStartup(StartupEventArgs e)
        {
            if (_host != null)
            {
                // 禁用休眠
                PreventSleep();
                await _host.StartAsync();
                var context = _host.Services.GetRequiredService<LocalDataContext>();
                context.LocalNodes.FirstOrDefault();
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
            if (_host != null)
            {
                // 停止 UDP 发现服务
                var discovery = _host.Services.GetRequiredService<UdpDiscoveryService>();
                discovery.Dispose();

                await _host.StopAsync();
                await Log.CloseAndFlushAsync();
            }
        }

    }

}
