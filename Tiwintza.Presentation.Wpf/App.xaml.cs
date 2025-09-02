using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Diagnostics;
using System.Windows.Markup;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Infrastructure.Services;
using Tiwintza.Infrastructure.Services.Auth;
using Tiwintza.Presentation.Wpf.Services;
using Tiwintza.Presentation.Wpf.Services.Windows;
using Tiwintza.Presentation.Wpf.ViewModels;
using Tiwintza.Presentation.Wpf.ViewModels.Activos;
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
                    cfg.SetBasePath(AppContext.BaseDirectory)
                       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                       .AddJsonFile("appsettings.template.json", optional: true, reloadOnChange: true)
                        #if DEBUG
                       .AddUserSecrets<App>(true)
                        #endif
                       .AddEnvironmentVariables();
                })
                .ConfigureServices((ctx, services) =>
                {
                    // DbContext desde configuración (UserSecrets/appsettings.json)
                    services.AddDbContext<AppDbContext>(opt =>
                        opt.UseNpgsql(ctx.Configuration.GetConnectionString("MainDb")));

                    // --- INICIO DE SERVICIOS DE INFRASTRUCTURE  ----
                    // Auth real
                    services.AddScoped<IAuthService, AuthService>();

                    //List para los activos
                    services.AddScoped<IActivosService, ActivosService>();

                    //Crud para los activos
                    services.AddScoped<IActivosCrudService, ActivosCrudService>();

                    // Auth de desarrollo (simula login sin validar)

                    // -- FIN DE SERVICIOS DE INFRASTRUCTURE  ----


                    //Credential  storage
                    services.AddSingleton<ICredentialStorage, WindowsCredentialStorage>();


                    // ViewModels
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<ActivosListViewModel>();
                    services.AddSingleton<MainViewModel>(sp =>
                        new MainViewModel(
                            () => sp.GetRequiredService<DashboardViewModel>(),
                            () => sp.GetRequiredService<ActivosListViewModel>()
                        ));

                    // Views
                    services.AddTransient<LoginWindow>();
                    services.AddSingleton<MainWindow>();

                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("es-EC")));
            await AppHost.StartAsync();
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
