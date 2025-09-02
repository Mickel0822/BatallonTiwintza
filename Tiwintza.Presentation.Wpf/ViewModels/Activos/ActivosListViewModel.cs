using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using Tiwintza.Infrastructure.Dtos;
using Tiwintza.Infrastructure.Services;

namespace Tiwintza.Presentation.Wpf.ViewModels.Activos;

public partial class ActivosListViewModel : ObservableObject
{
    private readonly IActivosService _svc;          // YA EXISTENTE (listado, filtros, KPIs)
    private readonly IActivosCrudService _crud;     // NUEVO (formulario CRUD)

    public ActivosListViewModel(IActivosService svc, IActivosCrudService crud)
    {
        _svc = svc;
        _crud = crud;
    }

    // ===== Navegación embebida (Listado <-> Form) =====
    [ObservableProperty] private bool isInForm;
    [ObservableProperty] private ActivoFormViewModel? formVm;

    // Filtros
    [ObservableProperty] private string? texto;
    [ObservableProperty] private long? areaId;
    [ObservableProperty] private long? estadoId;
    [ObservableProperty] private long? tipoId;
    [ObservableProperty] private bool incluirBaja;

    // Paginación
    [ObservableProperty] private int page = 1;
    [ObservableProperty] private int pageSize = 30;

    // Totales para KPIs
    [ObservableProperty] private int total;
    [ObservableProperty] private int totalOperativos;
    [ObservableProperty] private int totalBaja;

    // Total para la grilla
    [ObservableProperty] private int gridTotal;

    public int PageCount => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling((double)GridTotal / PageSize));
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < PageCount;

    public ObservableCollection<ActivoListItemDto> Items { get; } = new();

    // Catálogos para los ComboBox
    public ObservableCollection<IdNombreDto> Areas { get; } = new();
    public ObservableCollection<IdNombreDto> Estados { get; } = new();
    public ObservableCollection<IdNombreDto> Tipos { get; } = new();

    // === Inicialización ===
    public async Task InitAsync()
    {
        if (Areas.Count == 0)
        {
            var (areas, estados, tipos) = await _svc.CatalogosAsync();
            Areas.Clear(); foreach (var a in areas) Areas.Add(a);
            Estados.Clear(); foreach (var e in estados) Estados.Add(e);
            Tipos.Clear(); foreach (var t in tipos) Tipos.Add(t);
        }

        await CargarAsync();
    }

    [RelayCommand]
    public async Task CargarAsync()
    {
        var filtro = new ActivoFiltro
        {
            Texto = Texto,
            AreaId = AreaId,
            EstadoId = EstadoId,
            TipoId = TipoId,
            IncluirBaja = IncluirBaja,
            Page = Page,
            PageSize = PageSize
        };

        var r = await _svc.BuscarAsync(filtro);
        Items.Clear();
        foreach (var it in r.Items) Items.Add(it);
        GridTotal = r.Total;

        var (tot, ope, baja) = await _svc.ResumenAsync(filtro);
        Total = tot;
        TotalOperativos = ope;
        TotalBaja = baja;

        OnPropertyChanged(nameof(PageCount));
        OnPropertyChanged(nameof(HasPrev));
        OnPropertyChanged(nameof(HasNext));
    }

    [RelayCommand]
    public async Task IrPaginaAsync(int p)
    {
        var target = Math.Clamp(p, 1, PageCount);
        if (target == Page) return;
        Page = target;
        await CargarAsync();
    }

    [RelayCommand] public async Task AplicarAsync() { Page = 1; await CargarAsync(); }
    [RelayCommand] public Task PagAnteriorAsync() => IrPaginaAsync(Page - 1);
    [RelayCommand] public Task PagSiguienteAsync() => IrPaginaAsync(Page + 1);

    // ======= Navegación al formulario =======
    [RelayCommand]
    public async Task NuevoAsync()
    {
        FormVm = new ActivoFormViewModel(_crud);
        SuscribirEventosForm();
        await FormVm.InicializarCrearAsync();
        IsInForm = true;
    }

    // (opcional) editar desde la lista
    [RelayCommand]
    public async Task EditarAsync(long id)
    {
        FormVm = new ActivoFormViewModel(_crud);
        SuscribirEventosForm();
        await FormVm.InicializarEditarAsync(id);
        IsInForm = true;
    }

    private void SuscribirEventosForm()
    {
        if (FormVm is null) return;

        FormVm.Guardado += async _ =>
        {
            IsInForm = false;
            await CargarAsync();
        };
        FormVm.Cancelado += () => IsInForm = false;
        FormVm.VolverSolicitado += () => IsInForm = false;
    }

    partial void OnPageSizeChanged(int value) => _ = CargarAsync();

    // (placeholder de exportaciones)
    [RelayCommand] public Task ExportarExcelAsync() => Task.CompletedTask;
    [RelayCommand] public Task ExportarPdfAsync() => Task.CompletedTask;
}
