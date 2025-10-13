using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Tiwintza.Presentation.Wpf.Models;
using Tiwintza.Presentation.Wpf.ViewModels.Activos;
using Tiwintza.Presentation.Wpf.ViewModels.Existencias;

namespace Tiwintza.Presentation.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly Func<DashboardViewModel> _dashboardFactory;
    private readonly Func<ActivosListViewModel> _activosFactory;
    private readonly Func<ExistenciasListViewModel> _existenciasFactory;

    private readonly Dictionary<string, Func<object>> _factoryMap;
    private readonly Dictionary<string, object> _cache = new();

    [ObservableProperty] private object? current;
    [ObservableProperty] private NavigationItem? selectedMenu;

    public IReadOnlyList<NavigationItem> Menu { get; }

    public MainViewModel(Func<DashboardViewModel> dashboardFactory,
                         Func<ActivosListViewModel> activosFactory,
                         Func<ExistenciasListViewModel> existenciasFactory)
    {
        _dashboardFactory = dashboardFactory;
        _activosFactory = activosFactory;
        _existenciasFactory = existenciasFactory;

        _factoryMap = new()
        {
            ["dashboard"] = () => _dashboardFactory(),
            ["activos"] = () => _activosFactory(),
            ["existencias"] = () => _existenciasFactory(), 
        };

        Menu = new[]
        {
            new NavigationItem("dashboard","Dashboard","ViewDashboardOutline"),
            new NavigationItem("activos","Activos","Briefcase"),
            new NavigationItem("existencias","Existencias","Warehouse"),
            new NavigationItem("reportes","Reportes","ReportBoxOutline"),
            new NavigationItem("catalogos","Catalogos","TableCog"),
            new NavigationItem("auditoria","Auditoria","FileSearchOutline"),
            new NavigationItem("config","Configuracion","CogOutline"),
        };

        SelectedMenu = Menu.First();
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

        var dashboard = _dashboardFactory();
        return _cache["dashboard"] = dashboard;
    }

    [RelayCommand] private void IrDashboard() => SelectedMenu = Menu.First(m => m.Key == "dashboard");
    [RelayCommand] private void IrActivos() => SelectedMenu = Menu.First(m => m.Key == "activos");
    [RelayCommand] private void IrExistencias() => SelectedMenu = Menu.First(m => m.Key == "existencias");
}
