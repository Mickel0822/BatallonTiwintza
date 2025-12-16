using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Tiwintza.Infrastructure.Dtos;
using Tiwintza.Infrastructure.Dtos.Catalogos;
using Tiwintza.Infrastructure.Services;
using Tiwintza.Infrastructure.Services.Auth;

namespace Tiwintza.Presentation.Wpf.ViewModels.Activos;

public sealed partial class ActivosListViewModel : ObservableObject
{
    private readonly IActivosService _activosService;
    private readonly IActivosCrudService _crudService;
    private readonly ICatalogosService _catalogosService;
    private readonly IAuthService _authService;

    // Filtros
    [ObservableProperty] private string? textoBusqueda;
    [ObservableProperty] private CatalogoItemDto? areaSeleccionada;
    [ObservableProperty] private CatalogoItemDto? tipoSeleccionado;
    [ObservableProperty] private CatalogoItemDto? estadoSeleccionado;

    // Paginacion
    [ObservableProperty] private int page = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int total;

    // KPIs
    [ObservableProperty] private int totalOperativos;
    [ObservableProperty] private int totalBaja;

    // Estado UI
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private bool isInForm;
    [ObservableProperty] private bool isActionDialogOpen;
    [ObservableProperty] private bool isNotifyOpen;
    [ObservableProperty] private string? notifyTitle;
    [ObservableProperty] private string? notifyMessage;

    // Listas
    public ObservableCollection<ActivoListItemDto> Items { get; } = new();
    public ObservableCollection<CatalogoItemDto> Areas { get; } = new();
    public ObservableCollection<CatalogoItemDto> Tipos { get; } = new();
    public ObservableCollection<CatalogoItemDto> Estados { get; } = new();

    // Formulario (Generic object to handle different VMs)
    [ObservableProperty] private object? formVm;

    // Acciones
    [ObservableProperty] private ActivoListItemDto? accionSeleccionada;

    public bool HasNext => Page * PageSize < Total;
    public bool HasPrev => Page > 1;

    public bool IsAdmin => _authService.Current?.Roles.Contains("admin", StringComparer.OrdinalIgnoreCase) == true ||
                           _authService.Current?.Roles.Contains("administrador", StringComparer.OrdinalIgnoreCase) == true;

    public ActivosListViewModel(
        IActivosService activosService,
        IActivosCrudService crudService,
        ICatalogosService catalogosService,
        IAuthService authService)
    {
        _activosService = activosService;
        _crudService = crudService;
        _catalogosService = catalogosService;
        _authService = authService;
    }

    public async Task InitAsync()
    {
        await CargarCatalogosAsync();
        await BuscarAsync();
        OnPropertyChanged(nameof(IsAdmin)); // Ensure UI updates
    }

    private async Task CargarCatalogosAsync()
    {
        try 
        {
            var areas = await _catalogosService.ObtenerAreasAsync();
            Areas.Clear();
            foreach(var a in areas) Areas.Add(a);

            var tipos = await _catalogosService.ObtenerTiposAsync();
            Tipos.Clear();
            foreach(var t in tipos) Tipos.Add(t);

            // Hardcoded states for now as ObtenerEstadosAsync is missing
            Estados.Clear();
            Estados.Add(new CatalogoItemDto { Id = 1, Nombre = "Bueno" });
            Estados.Add(new CatalogoItemDto { Id = 2, Nombre = "Regular" });
            // Estados.Add(new CatalogoItemDto { Id = 3, Nombre = "Malo" }); // Excluded per requirements
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error cargando catálogos: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task BuscarAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var filtro = new ActivoFiltro
            {
                Texto = TextoBusqueda,
                AreaId = AreaSeleccionada?.Id,
                TipoId = TipoSeleccionado?.Id,
                EstadoId = EstadoSeleccionado?.Id,
                Page = Page,
                PageSize = PageSize
            };

            var result = await _activosService.BuscarAsync(filtro);
            
            Items.Clear();
            foreach(var item in result.Items) Items.Add(item);
            
            Total = result.Total;
            
            // KPIs - Commented out as GetStatsAsync is missing
            // var stats = await _activosService.GetStatsAsync();
            // TotalOperativos = stats.Operativos;
            // TotalBaja = stats.Baja;
            
            // Temporary placeholder
            TotalOperativos = 0;
            TotalBaja = 0;

            OnPropertyChanged(nameof(HasNext));
            OnPropertyChanged(nameof(HasPrev));
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error buscando activos: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void LimpiarFiltros()
    {
        TextoBusqueda = null;
        AreaSeleccionada = null;
        TipoSeleccionado = null;
        EstadoSeleccionado = null;
        Page = 1;
        BuscarCommand.Execute(null);
    }

    [RelayCommand]
    private async Task PagSiguiente()
    {
        if (HasNext)
        {
            Page++;
            await BuscarAsync();
        }
    }

    [RelayCommand]
    private async Task PagAnterior()
    {
        if (HasPrev)
        {
            Page--;
            await BuscarAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(IsAdmin))]
    private async Task Nuevo()
    {
        var vm = new ActivoFormViewModel(_crudService);
        await vm.InicializarCrearAsync();
        
        vm.Guardado += async (id) => 
        {
            IsInForm = false;
            MostrarNotificacion("Éxito", "Activo creado correctamente");
            await BuscarAsync();
        };
        vm.Cancelado += () => IsInForm = false;
        
        FormVm = vm;
        IsInForm = true;
    }

    [RelayCommand]
    private void MostrarAcciones(ActivoListItemDto item)
    {
        AccionSeleccionada = item;
        IsActionDialogOpen = true;
    }

    [RelayCommand]
    private void CancelarAcciones()
    {
        IsActionDialogOpen = false;
        AccionSeleccionada = null;
    }

    [RelayCommand(CanExecute = nameof(IsAdmin))]
    private async Task EditarSeleccionado()
    {
        if (AccionSeleccionada == null) return;
        var id = AccionSeleccionada.Id;
        IsActionDialogOpen = false;

        var vm = new ActivoFormViewModel(_crudService);
        await vm.InicializarEditarAsync(id);

        vm.Guardado += async (newId) => 
        {
            IsInForm = false;
            MostrarNotificacion("Éxito", "Activo actualizado correctamente");
            await BuscarAsync();
        };
        vm.Cancelado += () => IsInForm = false;

        FormVm = vm;
        IsInForm = true;
    }

    [RelayCommand(CanExecute = nameof(IsAdmin))]
    private async Task AbrirTraslado()
    {
        if (AccionSeleccionada == null) return;
        IsActionDialogOpen = false;

        var vm = new ActivoTrasladoViewModel(_crudService);
        vm.Initialize(AccionSeleccionada);
        await vm.InicializarAsync();

        vm.TrasladarSolicitado += async () =>
        {
            IsInForm = false;
            MostrarNotificacion("Éxito", "Activo trasladado correctamente");
            await BuscarAsync();
        };
        vm.VolverSolicitado += () => IsInForm = false;

        FormVm = vm;
        IsInForm = true;
    }

    [RelayCommand(CanExecute = nameof(IsAdmin))]
    private async Task AbrirBaja()
    {
        if (AccionSeleccionada == null) return;
        IsActionDialogOpen = false;

        var vm = new ActivoBajaViewModel(_crudService);
        vm.Initialize(AccionSeleccionada);
        await vm.InicializarAsync();

        vm.DarBajaSolicitado += async () =>
        {
            IsInForm = false;
            MostrarNotificacion("Éxito", "Activo dado de baja correctamente");
            await BuscarAsync();
        };
        vm.VolverSolicitado += () => IsInForm = false;

        FormVm = vm;
        IsInForm = true;
    }

    [RelayCommand]
    private async Task ExportarExcel()
    {
        try
        {
            IsBusy = true;
            
            var filtro = new ActivoFiltro
            {
                Texto = TextoBusqueda,
                AreaId = AreaSeleccionada?.Id,
                TipoId = TipoSeleccionado?.Id,
                EstadoId = EstadoSeleccionado?.Id,
                Page = 1,
                PageSize = int.MaxValue // Export all
            };

            var datos = await _activosService.ExportarMaestroAsync(filtro);
            
            if (datos.Count == 0)
            {
                MostrarNotificacion("Exportar Excel", "No hay datos para exportar.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"Activos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await Task.Run(() =>
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Activos");

                    // Encabezados
                    var headers = new[] 
                    { 
                        "Código", "Nombre", "Descripción", "Marca", "Modelo", "Serie", 
                        "Material", "Tipo", "Estado", "Area", "Valor Unitario", 
                        "Fecha Compra", "Proveedor", "Vida Útil (meses)", "Depreciación Mensual",
                        "Autorización", "Garantía (meses)", "Observaciones", "En Baja"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cell(1, i + 1).Value = headers[i];
                        ws.Cell(1, i + 1).Style.Font.Bold = true;
                        ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0xE2, 0xE8, 0xF0);
                    }

                    // Datos
                    for (int i = 0; i < datos.Count; i++)
                    {
                        var item = datos[i];
                        var row = i + 2;

                        ws.Cell(row, 1).Value = item.Codigo;
                        ws.Cell(row, 2).Value = item.Nombre;
                        ws.Cell(row, 3).Value = item.Descripcion;
                        ws.Cell(row, 4).Value = item.Marca;
                        ws.Cell(row, 5).Value = item.Modelo;
                        ws.Cell(row, 6).Value = item.Serie;
                        ws.Cell(row, 7).Value = item.Material;
                        ws.Cell(row, 8).Value = item.Tipo;
                        ws.Cell(row, 9).Value = item.Estado;
                        ws.Cell(row, 10).Value = item.Area;
                        ws.Cell(row, 11).Value = item.ValorUnitario;
                        ws.Cell(row, 11).Style.NumberFormat.Format = "$ #,##0.00";
                        
                        if (item.FechaCompra.HasValue)
                        {
                            ws.Cell(row, 12).Value = item.FechaCompra.Value.ToDateTime(TimeOnly.MinValue);
                            ws.Cell(row, 12).Style.DateFormat.Format = "dd/MM/yyyy";
                        }

                        ws.Cell(row, 13).Value = item.Proveedor;
                        ws.Cell(row, 14).Value = item.VidaUtilMeses;
                        ws.Cell(row, 15).Value = item.DepreciacionMensual;
                        ws.Cell(row, 15).Style.NumberFormat.Format = "$ #,##0.00";
                        ws.Cell(row, 16).Value = item.DocumentoAutorizacion;
                        ws.Cell(row, 17).Value = item.GarantiaMeses;
                        ws.Cell(row, 18).Value = item.Observaciones;
                        ws.Cell(row, 19).Value = item.EnBaja ? "SI" : "NO";
                    }

                    ws.Columns().AdjustToContents();
                    wb.SaveAs(dialog.FileName);
                });

                MostrarNotificacion("Éxito", $"Se exportaron {datos.Count} registros correctamente.");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error exportando: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void MostrarNotificacion(string title, string message)
    {
        NotifyTitle = title;
        NotifyMessage = message;
        IsNotifyOpen = true;
    }

    [RelayCommand]
    private void CerrarNotificacion()
    {
        IsNotifyOpen = false;
    }
}
