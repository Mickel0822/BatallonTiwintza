using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Presentation.Wpf.Services;
using Tiwintza.Presentation.Wpf.ViewModels;
using Tiwintza.Presentation.Wpf.Views;

namespace Tiwintza.Presentation.Wpf
{
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; } = null!;

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.SetBasePath(AppContext.BaseDirectory);
                    cfg.AddJsonFile("appsettings.template.json", optional: true, reloadOnChange: true);
                    cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    #if DEBUG
                    cfg.AddUserSecrets<App>();
                    #endif
                    cfg.AddEnvironmentVariables();
                })
                .ConfigureServices((ctx, services) =>
                {
                    var cs = ctx.Configuration.GetConnectionString("MainDb");
                    services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(cs));

                    // Servicios
                    services.AddSingleton<IAuthService, FakeAuthService>();

                    // ViewModels
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<AreasViewModel>();

                    // Ventanas
                    services.AddTransient<LoginWindow>();
                    services.AddTransient<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost.StartAsync();
            // Mostrar primero el Login
            var login = AppHost.Services.GetRequiredService<LoginWindow>();
            login.Show();
            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await AppHost.StopAsync();
            AppHost.Dispose();
            base.OnExit(e);
        }
    }
}
