using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Services.Auth;
using Tiwintza.Presentation.Wpf.Models;
using Tiwintza.Presentation.Wpf.Services.Navigation;
using Tiwintza.Presentation.Wpf.ViewModels.Activos;
using Tiwintza.Presentation.Wpf.ViewModels.Auditoria;
using Tiwintza.Presentation.Wpf.ViewModels.Catalogos;
using Tiwintza.Presentation.Wpf.ViewModels.Configuracion;
using Tiwintza.Presentation.Wpf.ViewModels.Existencias;

namespace Tiwintza.Presentation.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationCoordinator _navigator;
    private readonly IAuthService _auth;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly Func<DashboardViewModel> _dashboardFactory;
    private readonly Func<ActivosListViewModel> _activosFactory;
    private readonly Func<ExistenciasListViewModel> _existenciasFactory;
    private readonly Func<CatalogosViewModel> _catalogosFactory;
    private readonly Func<AuditoriaViewModel> _auditoriaFactory;
    private readonly Func<ConfiguracionViewModel> _configFactory;

    private readonly Dictionary<string, Func<object>> _factoryMap;
    private readonly Dictionary<string, object> _cache = new();
    private readonly ObservableCollection<NavigationItem> _menuItems = new();

    [ObservableProperty] private object? current;
    [ObservableProperty] private NavigationItem? selectedMenu;

    [ObservableProperty] private string? userDisplayName;
    [ObservableProperty] private string? userAlias;
    [ObservableProperty] private string? userRoleLabel;
    [ObservableProperty] private string? sedeActualNombre;

    [ObservableProperty] private bool isSedeSelectorOpen;
    [ObservableProperty] private IReadOnlyList<SedeTenant>? sedesDisponibles;
    [ObservableProperty] private SedeTenant? sedeSeleccionada;
    [ObservableProperty] private string? cambioSedeMensaje;

    public bool IsAdmin { get; private set; }

    public ObservableCollection<NavigationItem> Menu => _menuItems;

    public event Action? LogoutRequested;

    public MainViewModel(INavigationCoordinator navigator,
                         IAuthService auth,
                         ITenantAccessor tenantAccessor,
                         Func<DashboardViewModel> dashboardFactory,
                         Func<ActivosListViewModel> activosFactory,
                         Func<ExistenciasListViewModel> existenciasFactory,
                         Func<CatalogosViewModel> catalogosFactory,
                         Func<AuditoriaViewModel> auditoriaFactory,
                         Func<ConfiguracionViewModel> configFactory)
    {
        _navigator = navigator;
        _navigator.NavigationRequested += OnNavigationRequested;

        _auth = auth;
        _tenantAccessor = tenantAccessor;
        _dashboardFactory = dashboardFactory;
        _activosFactory = activosFactory;
        _existenciasFactory = existenciasFactory;
        _catalogosFactory = catalogosFactory;
        _auditoriaFactory = auditoriaFactory;
        _configFactory = configFactory;

        _factoryMap = new()
        {
            ["dashboard"] = () => _dashboardFactory(),
            ["activos"] = () => _activosFactory(),
            ["existencias"] = () => _existenciasFactory(),
            ["catalogos"] = () => _catalogosFactory(),
            ["auditoria"] = () => _auditoriaFactory(),
            ["config"] = () => _configFactory()
        };

        Application.Current.Dispatcher.Invoke(() => InitializeSessionState(_auth.Current));

        if (Menu.Count > 0)
        {
            SelectedMenu = Menu[0];
            Current = ResolveViewModel(SelectedMenu.Key);
        }
    }

    partial void OnSelectedMenuChanged(NavigationItem? value)
    {
        if (value is null) return;
        Current = ResolveViewModel(value.Key);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _auth.LogoutAsync();
        _tenantAccessor.Clear();
        CancelarCambioSede();
        LogoutRequested?.Invoke();
    }

    [RelayCommand]
    private void MostrarSelectorSede()
    {
        if (!IsAdmin)
        {
            CambioSedeMensaje = "El cambio de sede es una operación restringida exclusivamente para Administradores.\nSi requiere realizar esta acción, por favor contacte al personal autorizado.";
            IsSedeSelectorOpen = true;
            return;
        }

        var session = _auth.Current;
        var sedes = session?.Sedes;
        if (sedes is null || sedes.Count == 0)
        {
            CambioSedeMensaje = "No hay sedes disponibles.";
            SedesDisponibles = Array.Empty<SedeTenant>();
            SedeSeleccionada = null;
            IsSedeSelectorOpen = true;
            ConfirmarCambioSedeCommand.NotifyCanExecuteChanged();
            return;
        }

        SedesDisponibles = sedes;
        SedeSeleccionada = sedes.FirstOrDefault();
        CambioSedeMensaje = null;
        IsSedeSelectorOpen = true;
        ConfirmarCambioSedeCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void CancelarCambioSede()
    {
        IsSedeSelectorOpen = false;
        CambioSedeMensaje = null;
        SedesDisponibles = null;
        SedeSeleccionada = null;
        ConfirmarCambioSedeCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(PuedeConfirmarCambioSede))]
    private void ConfirmarCambioSede()
    {
        if (SedeSeleccionada is null) return;
        CambiarSede(SedeSeleccionada);
        CancelarCambioSede();
    }

    private bool PuedeConfirmarCambioSede() => IsSedeSelectorOpen && SedeSeleccionada is not null;

    private void InitializeSessionState(UserSession? session)
    {
        var roles = session?.Roles ?? Array.Empty<string>();

        UserDisplayName = !string.IsNullOrWhiteSpace(session?.FullName)
            ? session!.FullName
            : session?.UserName ?? "Usuario";

        UserAlias = session?.UserName is { Length: > 0 } user
            ? $"@{user}"
            : null;

        UserRoleLabel = roles.Length > 0
            ? string.Join(" | ", roles)
            : "Sin rol asignado";

        IsAdmin = roles.Any(IsAdminRole);
        OnPropertyChanged(nameof(IsAdmin));

        _menuItems.Clear();
        _menuItems.Add(new NavigationItem("dashboard", "Dashboard", "ViewDashboardOutline"));
        _menuItems.Add(new NavigationItem("activos", "Activos", "Briefcase"));
        _menuItems.Add(new NavigationItem("existencias", "Existencias", "Warehouse"));
        _menuItems.Add(new NavigationItem("catalogos", "Catálogos", "TableCog"));
        
        if (IsAdmin)
        {
            _menuItems.Add(new NavigationItem("auditoria", "Auditoría", "FileSearchOutline"));
            _menuItems.Add(new NavigationItem("config", "Configuración", "CogOutline"));
        }

        OnPropertyChanged(nameof(Menu));
        ActualizarSedeActual();
    }

    private object ResolveViewModel(string key)
    {
        if (_cache.TryGetValue(key, out var vm)) return vm;

        if (_factoryMap.TryGetValue(key, out var factory))
        {
            vm = factory();
            _cache[key] = vm;
            return vm;
        }

        var dashboard = _factoryMap["dashboard"]();
        _cache["dashboard"] = dashboard;
        return dashboard;
    }

    private void OnNavigationRequested(object? sender, NavigationRequestEventArgs e)
    {
        var vm = ResolveViewModel(e.Key);

        if (SelectedMenu?.Key != e.Key)
        {
            var target = Menu.FirstOrDefault(m => m.Key == e.Key);
            if (target is not null)
            {
                SelectedMenu = target;
            }
            else
            {
                Current = vm;
            }
        }
        else
        {
            Current = vm;
        }

        e.AfterNavigate?.Invoke(vm);
    }

    [RelayCommand] private void IrDashboard() => SelectedMenu = Menu.First(m => m.Key == "dashboard");
    [RelayCommand] private void IrActivos() => SelectedMenu = Menu.First(m => m.Key == "activos");
    [RelayCommand] private void IrExistencias() => SelectedMenu = Menu.First(m => m.Key == "existencias");

    [RelayCommand]
    private void IrConfiguracion()
    {
        if (!IsAdmin)
        {
            return;
        }

        var config = Menu.FirstOrDefault(m => m.Key == "config");
        if (config is not null)
        {
            SelectedMenu = config;
        }
    }

    public void RefrescarSesion()
    {
        _cache.Clear();
        InitializeSessionState(_auth.Current);

        if (Menu.Count > 0)
        {
            SelectedMenu = Menu[0];
        }
        else
        {
            Current = null;
        }
    }

    private void ActualizarSedeActual() => SedeActualNombre = _tenantAccessor.Current?.Nombre;

    private void CambiarSede(SedeTenant sede)
    {
        _tenantAccessor.Set(sede);
        _cache.Clear();
        ActualizarSedeActual();
        if (Menu.Count > 0)
        {
            SelectedMenu = Menu[0];
        }
    }

    partial void OnIsSedeSelectorOpenChanged(bool value) => ConfirmarCambioSedeCommand.NotifyCanExecuteChanged();
    partial void OnSedeSeleccionadaChanged(SedeTenant? value) => ConfirmarCambioSedeCommand.NotifyCanExecuteChanged();

    private static bool IsAdminRole(string role) =>
        role.Equals("administrador", StringComparison.OrdinalIgnoreCase) ||
        role.Equals("admin", StringComparison.OrdinalIgnoreCase);
}
