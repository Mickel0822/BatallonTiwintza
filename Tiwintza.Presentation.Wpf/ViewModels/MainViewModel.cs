using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Tiwintza.Presentation.Wpf.Models;
using Tiwintza.Presentation.Wpf.ViewModels.Activos; // ← donde está ActivosListViewModel

namespace Tiwintza.Presentation.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly Func<DashboardViewModel> _dashboardFactory;
    private readonly Func<ActivosListViewModel> _activosFactory;

    // mapa de fábricas y cache por clave de menú
    private readonly Dictionary<string, Func<object>> _factoryMap;
    private readonly Dictionary<string, object> _cache = new();

    [ObservableProperty] private object? current;                 // ← ContentControl.Content
    [ObservableProperty] private NavigationItem? selectedMenu;

    public IReadOnlyList<NavigationItem> Menu { get; }

    public MainViewModel(Func<DashboardViewModel> dashboardFactory,
                         Func<ActivosListViewModel> activosFactory)
    {
        _dashboardFactory = dashboardFactory;
        _activosFactory = activosFactory;

        _factoryMap = new()
        {
            ["dashboard"] = () => _dashboardFactory(),
            ["activos"] = () => _activosFactory(),
            // cuando agregues más pantallas, solo añade aquí:
            // ["existencias"] = () => _existenciasFactory(),
        };

        Menu = new[]
        {
            new NavigationItem("dashboard","Dashboard","ViewDashboardOutline"),
            new NavigationItem("activos","Activos","Briefcase"),
            new NavigationItem("existencias","Existencias","Warehouse"),
            new NavigationItem("reportes","Reportes", "ReportBoxOutline"),
            new NavigationItem("catalogos","Catálogos","TableCog"),
            new NavigationItem("auditoria","Auditoría","FileSearchOutline"),
            new NavigationItem("config","Configuración","CogOutline"),
        };

        // pantalla inicial
        SelectedMenu = Menu.First();                    // “dashboard”
        Current = ResolveViewModel(SelectedMenu.Key);
    }

    partial void OnSelectedMenuChanged(NavigationItem? value)
    {
        if (value is null) return;
        Current = ResolveViewModel(value.Key);
    }

    private object ResolveViewModel(string key)
    {
        if (_cache.TryGetValue(key, out var vm)) return vm;

        if (_factoryMap.TryGetValue(key, out var factory))
            return _cache[key] = factory();

        // fallback
        return _cache["dashboard"] = _dashboardFactory();
    }

    // Atajos opcionales (para botones/menú)
    [RelayCommand] private void IrDashboard() => SelectedMenu = Menu.First(m => m.Key == "dashboard");
    [RelayCommand] private void IrActivos() => SelectedMenu = Menu.First(m => m.Key == "activos");
}
