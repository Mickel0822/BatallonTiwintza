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

        private static string ExternalConfigPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "Tiwintza", "appsettings.json");

        public App()
        {
            // Asegura carpeta y JSON externo (si no existe, lo crea con plantilla)
            EnsureExternalConfigFile();

            AppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.SetBasePath(AppContext.BaseDirectory)
#if DEBUG
                       // En Debug puedes tener un appsettings.json junto al exe
                       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
#endif
                       // Plantilla opcional junto al exe (no contiene secretos)
                       .AddJsonFile("appsettings.template.json", optional: true, reloadOnChange: true)

                       // **CONFIG EXTERNA OBLIGATORIA** (se recarga si cambia)
                       .AddJsonFile(ExternalConfigPath, optional: false, reloadOnChange: true)

#if DEBUG
                       // En debug, UserSecrets puede sobreescribir valores
                       .AddUserSecrets<App>(optional: true)
#endif
                       .AddEnvironmentVariables();
                })
                .ConfigureServices((ctx, services) =>
                {
                    // ---- DBContext desde configuración (externa/secret/env) ----
                    var csApp = ctx.Configuration.GetConnectionString("AppDb");
                    var csMain = ctx.Configuration.GetConnectionString("MainDb");
                    var connString = !string.IsNullOrWhiteSpace(csApp) ? csApp : csMain;

                    if (string.IsNullOrWhiteSpace(connString))
                        throw new InvalidOperationException(
                            $"Falta la cadena de conexión 'ConnectionStrings:AppDb' (o 'MainDb'). " +
                            $"Revise/edite el archivo: {ExternalConfigPath}");

                    services.AddSingleton<ITenantAccessor, TenantAccessor>();
                    services.AddSingleton<IAppUserAccessor, AppUserAccessor>();
                    services.AddSingleton<AppUserConnectionInterceptor>();

                    void ConfigureAppDbContext(IServiceProvider sp, DbContextOptionsBuilder opt)
                    {
                        opt.UseNpgsql(connString);

                        if (ctx.HostingEnvironment.IsDevelopment())
                        {
                            opt.EnableDetailedErrors();
                            opt.EnableSensitiveDataLogging();
                        }

                        opt.AddInterceptors(sp.GetRequiredService<AppUserConnectionInterceptor>());
                    }

                    services.AddDbContext<AppDbContext>(ConfigureAppDbContext);
                    services.AddDbContextFactory<AppDbContext>(ConfigureAppDbContext);

                    // --- INICIO DE SERVICIOS DE INFRASTRUCTURE  ----
                    services.AddScoped<IAuthService, AuthService>();
                    services.AddScoped<IActivosService, ActivosService>();
                    services.AddScoped<IActivosCrudService, ActivosCrudService>();
                    services.AddScoped<IExistenciasService, ExistenciasService>();
                    services.AddScoped<IExistenciasCrudService, ExistenciasCrudService>();
                    services.AddScoped<IDashboardService, DashboardService>();
                    services.AddScoped<ICatalogosService, CatalogosService>();
                    services.AddScoped<IAuditoriaService, AuditoriaService>();
                    services.AddScoped<IUsuariosService, UsuariosService>();
                    // --- FIN DE SERVICIOS DE INFRASTRUCTURE  ----

                    // Credential storage
                    services.AddSingleton<ICredentialStorage, WindowsCredentialStorage>();
                    services.AddSingleton<INavigationCoordinator, NavigationCoordinator>();

                    // ViewModels
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

            // --- Verificación de conectividad a la BD (sin migrar) ---
            try
            {
                using var scope = AppHost.Services.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                await using var db = await factory.CreateDbContextAsync();

                // Abrimos/cerramos explícitamente para obtener errores claros
                await db.Database.OpenConnectionAsync();
                await db.Database.CloseConnectionAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo conectar a la base de datos.\n\n" +
                    $"Archivo de configuración:\n{ExternalConfigPath}\n\n" +
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

        /// <summary>
        /// Crea la carpeta y un appsettings.json externo con plantilla si no existe.
        /// </summary>
        private static void EnsureExternalConfigFile()
        {
            var dir = Path.GetDirectoryName(ExternalConfigPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(ExternalConfigPath))
            {
                var plantilla = """
                {
                  "ConnectionStrings": {
                    "AppDb": "Host=127.0.0.1;Port=5432;Database=batallon_tiwintza;Username=appuser;Password=CAMBIAR;Pooling=true;Timeout=30;Include Error Detail=true"
                  }
                }
                """;
                File.WriteAllText(ExternalConfigPath, plantilla);
            }
        }
    }
}



