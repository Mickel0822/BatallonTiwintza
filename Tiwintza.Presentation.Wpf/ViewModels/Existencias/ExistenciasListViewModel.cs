using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Dtos.Existencias;
using Tiwintza.Infrastructure.Services;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace Tiwintza.Presentation.Wpf.ViewModels.Existencias;

public sealed partial class ExistenciasListViewModel : ObservableObject
{
    private readonly IExistenciasService _service;
    private readonly IExistenciasCrudService _crudService;
    private bool _initialized;

    public ExistenciasListViewModel(IExistenciasService service, IExistenciasCrudService crudService)
    {
        _service = service;
        _crudService = crudService;
        Items = new ObservableCollection<ExistenciaListItemDto>();
    }

    public ObservableCollection<ExistenciaListItemDto> Items { get; }

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string? textoBusqueda;
    [ObservableProperty] private int page = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int total;
    
    // Filtros adicionales
    [ObservableProperty] private ExistenciaNivelEstado? nivelSeleccionado;
    
    public ObservableCollection<NivelOption> NivelOpciones { get; } = new()
    {
        new(null, "Todos"),
        new(ExistenciaNivelEstado.Seguro, "Seguro"),
        new(ExistenciaNivelEstado.Bajo, "Bajo"),
        new(ExistenciaNivelEstado.Critico, "Critico"),
        new(ExistenciaNivelEstado.Excedido, "Excedido")
    };

    // Orquestación de formularios
    [ObservableProperty] private object? formVm;
    [ObservableProperty] private bool isInForm;

    public int PageCount => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling((double)Total / PageSize));
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < PageCount;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await CargarAsync();
        _initialized = true;
    }
    
    // Alias para compatibilidad si se llama InitAsync
    public async Task InitAsync() => await InitializeAsync();

    [RelayCommand]
    private async Task BuscarAsync()
    {
        Page = 1;
        await CargarAsync();
    }

    [RelayCommand]
    private async Task LimpiarFiltrosAsync()
    {
        TextoBusqueda = null;
        NivelSeleccionado = null;
        Page = 1;
        await CargarAsync();
    }
    
    // El metodo LimpiarFiltrosAsync genera LimpiarFiltrosCommand automaticamente al quitar el sufijo Async

    [RelayCommand]
    private async Task PaginaAnteriorAsync()
    {
        if (!HasPrevious) return;
        Page--;
        await CargarAsync();
    }
    
    // Alias para la vista
    [RelayCommand]
    private async Task PagAnterior() => await PaginaAnteriorAsync();

    [RelayCommand]
    private async Task PaginaSiguienteAsync()
    {
        if (!HasNext) return;
        Page++;
        await CargarAsync();
    }
    
    // Alias para la vista
    [RelayCommand]
    private async Task PagSiguiente() => await PaginaSiguienteAsync();

    [RelayCommand]
    private async Task NuevoIngresoAsync()
    {
        var vm = new ExistenciaIngresoViewModel(_crudService);
        vm.VolverSolicitado += CerrarFormulario;
        vm.Guardado += async (_) => 
        {
            CerrarFormulario();
            await CargarAsync();
            // Aquí se podría mostrar una notificación de éxito
        };
        vm.Cancelado += CerrarFormulario;
        
        await vm.InicializarAsync();
        
        FormVm = vm;
        IsInForm = true;
    }

    [RelayCommand]
    private async Task NuevaSalidaAsync()
    {
        var vm = new ExistenciaSalidaViewModel(_crudService, _service);
        vm.VolverSolicitado += CerrarFormulario;
        vm.Guardado += async (_) => 
        {
            CerrarFormulario();
            await CargarAsync();
        };
        vm.Cancelado += CerrarFormulario;
        
        await vm.InicializarAsync();
        
        FormVm = vm;
        IsInForm = true;
    }
    
    // Alias para compatibilidad con XAML
    public string? Texto
    {
        get => TextoBusqueda;
        set
        {
            if (SetProperty(ref textoBusqueda, value))
            {
                OnPropertyChanged(nameof(TextoBusqueda));
            }
        }
    }

    // Propiedades para notificaciones
    [ObservableProperty] private bool isNotifyOpen;
    [ObservableProperty] private string? notifyTitle;
    [ObservableProperty] private string? notifyMessage;
    [ObservableProperty] private bool notifyIsError;

    [RelayCommand]
    private void CerrarNotificacion()
    {
        IsNotifyOpen = false;
    }

    private void MostrarNotificacion(string titulo, string mensaje, bool esError = false)
    {
        NotifyTitle = titulo;
        NotifyMessage = mensaje;
        NotifyIsError = esError;
        IsNotifyOpen = true;
    }

    [RelayCommand]
    private async Task ExportarExcelAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var filtro = new ExistenciaFiltro
            {
                Texto = string.IsNullOrWhiteSpace(TextoBusqueda) ? null : TextoBusqueda.Trim(),
                Nivel = NivelSeleccionado,
                Page = 1,
                PageSize = int.MaxValue // Exportar todo
            };

            var datos = await _service.ExportarAsync(filtro);

            if (datos.Count == 0)
            {
                MostrarNotificacion("Exportar Excel", "No hay datos para exportar.", true);
                return;
            }

            var dialog = new SaveFileDialog
            {
                FileName = $"Existencias_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                DefaultExt = ".xlsx",
                Filter = "Excel Workbook (.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            await Task.Run(() =>
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Existencias");

                // Encabezados
                ws.Cell(1, 1).Value = "Código";
                ws.Cell(1, 2).Value = "Nombre";
                ws.Cell(1, 3).Value = "Unidad";
                ws.Cell(1, 4).Value = "Stock Actual";
                ws.Cell(1, 5).Value = "Nivel Estado";
                ws.Cell(1, 6).Value = "Proveedor";

                // Datos
                for (var i = 0; i < datos.Count; i++)
                {
                    var item = datos[i];
                    var row = i + 2;
                    ws.Cell(row, 1).Value = item.Codigo;
                    ws.Cell(row, 2).Value = item.Nombre;
                    ws.Cell(row, 3).Value = item.Unidad;
                    ws.Cell(row, 4).Value = item.StockActual;
                    ws.Cell(row, 5).Value = item.NivelEstado.ToString();
                    ws.Cell(row, 6).Value = item.Proveedor;
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(dialog.FileName);
            });

            MostrarNotificacion("Exportar Excel", "Archivo guardado correctamente.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Exportar Excel", $"Error al exportar: {ex.Message}", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportarPdfAsync()
    {
        // Implementación pendiente o mock
        await Task.CompletedTask;
        MostrarNotificacion("Exportar PDF", "Funcionalidad no implementada aún.");
    }

    private void CerrarFormulario()
    {
        IsInForm = false;
        FormVm = null;
    }

    private async Task CargarAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var filtro = new ExistenciaFiltro
            {
                Texto = string.IsNullOrWhiteSpace(TextoBusqueda) ? null : TextoBusqueda.Trim(),
                Nivel = NivelSeleccionado,
                Page = Page,
                PageSize = PageSize
            };

            var resultado = await _service.BuscarAsync(filtro);

            Items.Clear();
            foreach (var item in resultado.Items)
            {
                Items.Add(item);
            }

            Total = resultado.Total;
            PageSize = resultado.PageSize;
            Page = resultado.Page;

            OnPropertyChanged(nameof(PageCount));
            OnPropertyChanged(nameof(HasPrevious));
            OnPropertyChanged(nameof(HasNext));
            
            // Propiedades para la vista (alias)
            OnPropertyChanged(nameof(HasPrev));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    public bool HasPrev => HasPrevious;
}

public record NivelOption(ExistenciaNivelEstado? Valor, string Texto);
