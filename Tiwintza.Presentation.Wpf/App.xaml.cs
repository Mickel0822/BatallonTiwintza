using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Infrastructure.Data.Interceptors;
using Tiwintza.Infrastructure.Services;
using Tiwintza.Infrastructure.Services.Auth;
using Tiwintza.Presentation.Wpf.Services;
using Tiwintza.Presentation.Wpf.Services.Navigation;
using Tiwintza.Presentation.Wpf.ViewModels.Catalogos;
using Tiwintza.Presentation.Wpf.ViewModels.Auditoria;
using Tiwintza.Presentation.Wpf.ViewModels.Configuracion;
using Tiwintza.Presentation.Wpf.Services.Windows;
using Tiwintza.Presentation.Wpf.ViewModels;
using Tiwintza.Presentation.Wpf.ViewModels.Activos;
using Tiwintza.Presentation.Wpf.ViewModels.Existencias;
using Tiwintza.Presentation.Wpf.Views.Existencias;
using Tiwintza.Presentation.Wpf.Views;
using Tiwintza.Infrastructure;
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
                       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                       .AddEnvironmentVariables();
                })
                .ConfigureServices((ctx, services) =>
                {
                    var csApp = ctx.Configuration.GetConnectionString("AppDb");
                    var csMain = ctx.Configuration.GetConnectionString("MainDb");
                    var connString = !string.IsNullOrWhiteSpace(csApp) ? csApp : csMain;
                    if (string.IsNullOrWhiteSpace(connString))
                        throw new InvalidOperationException(
                            "Falta la cadena de conexión 'ConnectionStrings:AppDb' (o 'MainDb') en appsettings.json");
                    services.AddSingleton<ITenantAccessor, TenantAccessor>();
                    services.AddSingleton<IAppUserAccessor, AppUserAccessor>();
                    services.AddSingleton<AppUserConnectionInterceptor>();
                    void ConfigureAppDbContext(IServiceProvider sp, DbContextOptionsBuilder opt)
                    {
                        opt.UseNpgsql(connString);
                        opt.AddInterceptors(sp.GetRequiredService<AppUserConnectionInterceptor>());
                    }
                    services.AddDbContext<AppDbContext>(ConfigureAppDbContext);
                    services.AddDbContextFactory<AppDbContext>(ConfigureAppDbContext);
                    services.AddScoped<IAuthService, AuthService>();
                    services.AddScoped<IActivosService, ActivosService>();
                    services.AddScoped<IActivosCrudService, ActivosCrudService>();
                    services.AddScoped<IExistenciasService, ExistenciasService>();
                    services.AddScoped<IExistenciasCrudService, ExistenciasCrudService>();
                    services.AddScoped<IDashboardService, DashboardService>();
                    services.AddScoped<ICatalogosService, CatalogosService>();
                    services.AddScoped<IAuditoriaService, AuditoriaService>();
                    services.AddScoped<IUsuariosService, UsuariosService>();
                    services.AddSingleton<ICredentialStorage, WindowsCredentialStorage>();
                    services.AddSingleton<INavigationCoordinator, NavigationCoordinator>();
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<ActivosListViewModel>();
                    services.AddTransient<ExistenciasListViewModel>();
                    services.AddTransient<ExistenciaIngresoViewModel>();
                    services.AddTransient<ExistenciaSalidaViewModel>();
                    services.AddTransient<CatalogosViewModel>();
                    services.AddTransient<AuditoriaViewModel>();
                    services.AddTransient<ConfiguracionViewModel>();
                    services.AddTransient<Func<ExistenciaIngresoViewModel>>(sp =>
                            () => sp.GetRequiredService<ExistenciaIngresoViewModel>());
                    services.AddTransient<Func<ExistenciaSalidaViewModel>>(sp =>
                            () => sp.GetRequiredService<ExistenciaSalidaViewModel>());
                    services.AddSingleton<MainViewModel>(sp =>
                        new MainViewModel(
                            sp.GetRequiredService<INavigationCoordinator>(),
                            sp.GetRequiredService<IAuthService>(),
                            sp.GetRequiredService<ITenantAccessor>(),
                            () => sp.GetRequiredService<DashboardViewModel>(),
                            () => sp.GetRequiredService<ActivosListViewModel>(),
                            () => sp.GetRequiredService<ExistenciasListViewModel>(),
                            () => sp.GetRequiredService<CatalogosViewModel>(),
                            () => sp.GetRequiredService<AuditoriaViewModel>(),
                            () => sp.GetRequiredService<ConfiguracionViewModel>()));
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
            try
            {
                using var scope = AppHost.Services.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                await using var db = await factory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                await db.Database.CloseConnectionAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo conectar a la base de datos.\n\n" +
                    $"Detalle:\n{ex.Message}",
                    "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
                return;
            }
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