using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Dtos.Dashboard;
using Tiwintza.Infrastructure.Services;
using Tiwintza.Presentation.Wpf.Services.Navigation;
using Tiwintza.Presentation.Wpf.ViewModels.Activos;
using Tiwintza.Presentation.Wpf.ViewModels.Existencias;

namespace Tiwintza.Presentation.Wpf.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService;
    private readonly INavigationCoordinator _navigator;
    private bool _initialized;
    private bool _areasLoaded;

    public DashboardViewModel(IDashboardService dashboardService,
                              INavigationCoordinator navigator)
    {
        _dashboardService = dashboardService;
        _navigator = navigator;

        Movimientos = new ObservableCollection<DashboardMovimientoItem>();
        Areas = new ObservableCollection<AreaOptionDto>();

        KpiActivos = new KpiCardModel("Activos operativos");
        KpiCriticos = new KpiCardModel("Items en critico");
        KpiBajas = new KpiCardModel("Bajas del mes");
    }

    public KpiCardModel KpiActivos { get; }
    public KpiCardModel KpiCriticos { get; }
    public KpiCardModel KpiBajas { get; }

    public ObservableCollection<DashboardMovimientoItem> Movimientos { get; }
    public ObservableCollection<AreaOptionDto> Areas { get; }

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isAreaDialogOpen;
    [ObservableProperty] private bool isAreaDialogBusy;
    [ObservableProperty] private long? areaSeleccionadaId;
    [ObservableProperty] private string? areaDialogMessage;

    [ObservableProperty] private bool isNotifyOpen;
    [ObservableProperty] private string? notifyTitle;
    [ObservableProperty] private string? notifyMessage;
    [ObservableProperty] private bool notifyIsError;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await CargarResumenAsync();
        _initialized = true;
    }

    [RelayCommand]
    private async Task RefrescarAsync()
    {
        await CargarResumenAsync();
    }

    [RelayCommand]
    private void RegistrarActivo()
    {
        _navigator.Navigate("activos", vm =>
        {
            if (vm is ActivosListViewModel activos)
            {
                _ = activos.NuevoCommand.ExecuteAsync(null);
            }
        });
    }

    [RelayCommand]
    private void RegistrarIngreso()
    {
        _navigator.Navigate("existencias", vm =>
        {
            if (vm is ExistenciasListViewModel existencias)
            {
                _ = existencias.NuevoIngresoCommand.ExecuteAsync(null);
            }
        });
    }

    [RelayCommand]
    private async Task MostrarDialogoReporteAreaAsync()
    {
        if (IsAreaDialogOpen) return;

        await EnsureAreasAsync();
        if (Areas.Count == 0)
        {
            AreaDialogMessage = "No hay areas registradas";
            return;
        }

        AreaSeleccionadaId = Areas.First().Id;
        AreaDialogMessage = null;
        IsAreaDialogOpen = true;
    }

    [RelayCommand]
    private void CancelarReporteArea()
    {
        IsAreaDialogOpen = false;
        AreaDialogMessage = null;
    }

    [RelayCommand]
    private async Task GenerarReporteAreaAsync()
    {
        if (AreaSeleccionadaId is null)
        {
            AreaDialogMessage = "Seleccione un area";
            return;
        }

        IsAreaDialogBusy = true;
        try
        {
            var datos = await _dashboardService.ObtenerActivosPorAreaAsync(AreaSeleccionadaId.Value);
            if (datos.Count == 0)
            {
                AreaDialogMessage = "El area seleccionada no tiene activos registrados.";
                return;
            }

            var areaNombre = datos[0].Area;
            var dialog = new SaveFileDialog
            {
                Title = "Exportar activos por area",
                Filter = "Libro de Excel (*.xlsx)|*.xlsx",
                FileName = $"Activos_{SanitizeFileName(areaNombre)}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            await Task.Run(() => ExportarActivosAreaExcel(dialog.FileName, areaNombre, datos));

            IsAreaDialogOpen = false;
            MostrarNotificacion("Reporte generado", $"Se exportaron {datos.Count} activos de {areaNombre}.");
        }
        catch (Exception ex)
        {
            AreaDialogMessage = ex.Message;
        }
        finally
        {
            IsAreaDialogBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportarMovimientosAsync()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        try
        {
            IsBusy = true;
            var datos = await _dashboardService.ObtenerMovimientosDelDiaAsync(hoy);
            if (datos.Count == 0)
            {
                MostrarNotificacion("Sin datos", "No existen movimientos registrados en el dia.", true);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Exportar movimientos",
                Filter = "Libro de Excel (*.xlsx)|*.xlsx",
                FileName = $"Movimientos_{hoy:yyyyMMdd}.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            await Task.Run(() => ExportarMovimientosExcel(dialog.FileName, datos));

            MostrarNotificacion("Exportacion exitosa", $"Se exportaron {datos.Count} movimientos.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error al exportar", ex.Message, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CerrarNotificacion()
    {
        IsNotifyOpen = false;
    }

    private async Task CargarResumenAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var resumen = await _dashboardService.ObtenerResumenAsync();

            ActualizarKpi(KpiActivos, resumen.ActivosOperativos);
            ActualizarKpi(KpiCriticos, resumen.ItemsCriticos);
            ActualizarKpi(KpiBajas, resumen.BajasDelMes);

            Movimientos.Clear();
            foreach (var movimiento in resumen.UltimosMovimientos)
            {
                Movimientos.Add(new DashboardMovimientoItem(movimiento));
            }
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", ex.Message, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task EnsureAreasAsync()
    {
        if (_areasLoaded) return;
        try
        {
            IsAreaDialogBusy = true;
            Areas.Clear();
            var areas = await _dashboardService.ObtenerAreasAsync();
            foreach (var area in areas)
            {
                Areas.Add(area);
            }
            _areasLoaded = true;
        }
        catch (Exception ex)
        {
            AreaDialogMessage = ex.Message;
        }
        finally
        {
            IsAreaDialogBusy = false;
        }
    }

    private static void ActualizarKpi(KpiCardModel kpi, DashboardKpiDto dto)
    {
        kpi.Title = dto.Title;
        kpi.Actual = dto.Actual;
        kpi.Previous = dto.Previous;
    }

    private static void ExportarActivosAreaExcel(string ruta, string areaNombre, IReadOnlyList<ActivoPorAreaDto> datos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Activos");

        ws.Cell(1, 1).Value = $"Activos del area: {areaNombre}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 6).Merge();

        var headers = new[] { "Codigo", "Nombre", "Tipo", "Estado", "Valor unitario", "Fecha compra" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xE2, 0xE8, 0xF0);
        }

        for (var index = 0; index < datos.Count; index++)
        {
            var row = index + 4;
            var item = datos[index];
            ws.Cell(row, 1).Value = item.Codigo;
            ws.Cell(row, 2).Value = item.Nombre;
            ws.Cell(row, 3).Value = item.Tipo;
            ws.Cell(row, 4).Value = item.Estado;
            ws.Cell(row, 5).Value = item.ValorUnitario;
            ws.Cell(row, 5).Style.NumberFormat.Format = "$ #,##0.00";
            if (item.FechaCompra.HasValue)
            {
                ws.Cell(row, 6).Value = item.FechaCompra.Value.ToDateTime(TimeOnly.MinValue);
                ws.Cell(row, 6).Style.DateFormat.Format = "dd/MM/yyyy";
            }
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(ruta);
    }

    private static void ExportarMovimientosExcel(string ruta, IReadOnlyList<DashboardMovimientoDto> datos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Movimientos");

        var headers = new[] { "Codigo", "Fecha", "Tipo", "Descripcion", "Usuario", "Items" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xE2, 0xE8, 0xF0);
        }

        for (var index = 0; index < datos.Count; index++)
        {
            var row = index + 2;
            var item = datos[index];
            ws.Cell(row, 1).Value = item.Codigo;
            ws.Cell(row, 2).Value = item.Fecha;
            ws.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            ws.Cell(row, 3).Value = item.Tipo.ToString();
            ws.Cell(row, 4).Value = item.Descripcion;
            ws.Cell(row, 5).Value = item.Usuario;
            ws.Cell(row, 6).Value = item.Items ?? 0;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(ruta);
    }

    private void MostrarNotificacion(string titulo, string mensaje, bool esError = false)
    {
        NotifyTitle = titulo;
        NotifyMessage = mensaje;
        NotifyIsError = esError;
        IsNotifyOpen = true;
    }

    private static string SanitizeFileName(string nombre)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            nombre = nombre.Replace(invalid, '_');
        }
        return nombre;
    }

    public sealed class DashboardMovimientoItem
    {
        public DashboardMovimientoItem(DashboardMovimientoDto dto)
        {
            Codigo = dto.Codigo;
            Fecha = dto.Fecha;
            Tipo = dto.Tipo;
            Descripcion = dto.Descripcion;
            Usuario = dto.Usuario;
            Items = dto.Items;
        }

        public string Codigo { get; }
        public DateTime Fecha { get; }
        public DashboardMovimientoTipo Tipo { get; }
        public string Descripcion { get; }
        public string? Usuario { get; }
        public int? Items { get; }

        public string FechaTexto => Fecha.ToString("dd/MM/yyyy");
        public string TipoTexto => Tipo switch
        {
            DashboardMovimientoTipo.Ingreso => "Ingreso",
            DashboardMovimientoTipo.Salida => "Salida",
            DashboardMovimientoTipo.Traslado => "Traslado",
            DashboardMovimientoTipo.Baja => "Baja",
            _ => Tipo.ToString()
        };

        public string ChipBrushKey => Tipo switch
        {
            DashboardMovimientoTipo.Ingreso => "StatusSuccessBrush",
            DashboardMovimientoTipo.Salida => "StatusInfoBrush",
            DashboardMovimientoTipo.Traslado => "StatusWarningBrush",
            DashboardMovimientoTipo.Baja => "StatusDangerBrush",
            _ => "StatusInfoBrush"
        };
    }

    public sealed class KpiCardModel : ObservableObject
    {
        private string _title;
        private int _actual;
        private int? _previous;

        public KpiCardModel(string title)
        {
            _title = title;
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public int Actual
        {
            get => _actual;
            set
            {
                if (SetProperty(ref _actual, value))
                {
                    OnPropertyChanged(nameof(Variation));
                    OnPropertyChanged(nameof(VariationText));
                    OnPropertyChanged(nameof(IsPositive));
                }
            }
        }

        public int? Previous
        {
            get => _previous;
            set
            {
                if (SetProperty(ref _previous, value))
                {
                    OnPropertyChanged(nameof(Variation));
                    OnPropertyChanged(nameof(VariationText));
                    OnPropertyChanged(nameof(IsPositive));
                }
            }
        }

        public double? Variation
        {
            get
            {
                if (Previous is null || Previous == 0)
                {
                    return Actual == 0 ? 0 : (double?)null;
                }

                return (Actual - Previous.Value) / (double)Previous.Value * 100d;
            }
        }

        public string VariationText => Variation switch
        {
            null => "Sin referencia",
            0 => "0% vs. periodo anterior",
            > 0 => $"+{Variation:0.0}% vs. periodo anterior",
            < 0 => $"{Variation:0.0}% vs. periodo anterior",
            _ => "0% vs. periodo anterior"
        };

        public bool? IsPositive => Variation switch
        {
            null => null,
            0 => null,
            > 0 => true,
            < 0 => false,
            _ => null
        };
    }
}


