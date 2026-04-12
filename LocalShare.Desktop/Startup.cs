//using LocalShare.Desktop.DataContext;
//using LocalShare.Desktop.KeepStates;
//using LocalShare.Desktop.Server;
//using LocalShare.Desktop.ViewModels;
//using LocalShare.Desktop.Views;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace LocalShare.Desktop
//{
//    public class Startup
//    {
//        public void ConfigureServices(IServiceCollection services)
//        {
//            services.AddSingleton<MainViewModel>();
//            services.AddSingleton<MainWindow>();
//            services.AddSingleton<UdpDiscoveryService>();
//            services.AddSingleton<HomeDataHolder>();

//            services.AddTransient<HomeView>();
//            services.AddTransient<HomeViewModel>();
//            services.AddTransient<ReceiveView>();
//            services.AddTransient<ReceiveViewModel>();
//            services.AddTransient<SendView>();
//            services.AddTransient<SendViewModel>();

//            var dbPath = Path.Combine(AppContext.BaseDirectory, "Db", "LocalShare.db");
//            services.AddDbContext<LocalDataContext>(opt =>
//            {
//                opt.UseSqlite($"Data Source={dbPath}");
//            });

//            //services.AddGrpc();
//        }

//        //public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
//        //{
//        //    app.UseRouting();
//        //    app.UseEndpoints(endpoints =>
//        //    {
//        //        //endpoints.MapGrpcService<LocalServer>();
//        //    });
//        //}

//    }
//}
