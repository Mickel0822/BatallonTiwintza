using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using ClosedXML.Excel;
using Tiwintza.Infrastructure.Dtos.Existencias;
using Tiwintza.Infrastructure.Services;

namespace Tiwintza.Presentation.Wpf.ViewModels.Existencias;

public sealed partial class ExistenciasListViewModel : ObservableObject
{
    private readonly IExistenciasService _svc;
    private readonly Func<ExistenciaIngresoViewModel> _ingresoFactory;
    private readonly Func<ExistenciaSalidaViewModel> _salidaFactory;

    private bool _inited;

    public ExistenciasListViewModel(IExistenciasService svc,
                                    Func<ExistenciaIngresoViewModel> ingresoFactory,
                                    Func<ExistenciaSalidaViewModel> salidaFactory)
    {
        _svc = svc;
        _ingresoFactory = ingresoFactory;
        _salidaFactory = salidaFactory;

        Items = new ObservableCollection<ExistenciaListItemDto>();

        NivelOpciones = new ReadOnlyCollection<NivelOption>(new[]
        {
            new NivelOption("Todos", null),
            new NivelOption("Crítico", ExistenciaNivelEstado.Critico),
            new NivelOption("Bajo", ExistenciaNivelEstado.Bajo),
            new NivelOption("Seguro", ExistenciaNivelEstado.Seguro),
            new NivelOption("Normal", ExistenciaNivelEstado.Normal),
            new NivelOption("Excedido", ExistenciaNivelEstado.Excedido)
        });

        NivelSeleccionado = NivelOpciones.First();
    }

    public ObservableCollection<ExistenciaListItemDto> Items { get; }

    public IReadOnlyList<NivelOption> NivelOpciones { get; }

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isInForm;
    [ObservableProperty] private object? formVm;
    [ObservableProperty] private string? errorMessage;

    [ObservableProperty] private string? notifyTitle;
    [ObservableProperty] private string? notifyMessage;
    [ObservableProperty] private bool isNotifyOpen;

    [ObservableProperty] private string? texto;
    [ObservableProperty] private NivelOption? nivelSeleccionado;

    [ObservableProperty] private int page = 1;
    [ObservableProperty] private int pageSize = 30;
    [ObservableProperty] private int total;

    public int PageCount => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling((double)Total / PageSize));
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < PageCount;

    public async Task InitAsync()
    {
        if (_inited) return;
        _inited = true;
        await CargarAsync(resetPage: true);
    }

    [RelayCommand]
    private async Task BuscarAsync()
    {
        Page = 1;
        await CargarAsync();
    }

    [RelayCommand]
    private async Task LimpiarFiltrosAsync()
    {
        if (IsBusy) return;
        Texto = null;
        NivelSeleccionado = NivelOpciones.FirstOrDefault();
        Page = 1;
        await CargarAsync();
    }

        [RelayCommand]
    private async Task RefrescarAsync()
    {
        await CargarAsync();
    }

    [RelayCommand]
    private async Task NuevoIngresoAsync()
    {
        if (IsBusy) return;

        var vm = _ingresoFactory();
        vm.Guardado += async _ =>
        {
            await MostrarNotificacionAsync("Ingreso registrado", "El stock fue actualizado correctamente.");
            CerrarFormulario();
            await CargarAsync();
        };
        vm.Cancelado += CerrarFormulario;
        vm.VolverSolicitado += CerrarFormulario;

        await vm.InicializarAsync();

        FormVm = vm;
        IsInForm = true;
    }

    [RelayCommand]
    private async Task NuevaSalidaAsync()
    {
        if (IsBusy) return;

        var vm = _salidaFactory();
        vm.Guardado += async _ =>
        {
            await MostrarNotificacionAsync("Salida registrada", "El stock fue actualizado correctamente.");
            CerrarFormulario();
            await CargarAsync();
        };
        vm.Cancelado += CerrarFormulario;
        vm.VolverSolicitado += CerrarFormulario;

        await vm.InicializarAsync();

        FormVm = vm;
        IsInForm = true;
    }

    [RelayCommand]
    private void CerrarNotificacion()
    {
        IsNotifyOpen = false;
        NotifyTitle = null;
        NotifyMessage = null;
    }

    [RelayCommand]
    private async Task ExportarExcelAsync()
    {
        try
        {
            var filtro = CrearFiltro(includePaging: false);
            var datos = await _svc.ExportarAsync(filtro);

            if (datos.Count == 0)
            {
                await MostrarNotificacionAsync("Sin resultados", "No se encontraron existencias para exportar con el filtro actual.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Exportar existencias a Excel",
                Filter = "Libro de Excel (*.xlsx)|*.xlsx",
                FileName = $"Existencias_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx",
                OverwritePrompt = true
            };

            var result = dialog.ShowDialog();
            if (result != true) return;

            var ruta = dialog.FileName;

            await Task.Run(() =>
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Existencias");

                var headers = new[]
                {
                    "Codigo",
                    "Nombre",
                    "Unidad",
                    "Stock",
                    "Nivel seguro",
                    "Nivel maximo",
                    "Nivel minimo",
                    "Nivel critico",
                    "Estado",
                    "Proveedor"
                };

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
                    ws.Cell(row, 2).Value = item.Nombre;
                    ws.Cell(row, 3).Value = item.Unidad;
                    ws.Cell(row, 4).Value = item.StockActual;
                    ws.Cell(row, 5).Value = item.NivelSeguridad;
                    ws.Cell(row, 6).Value = item.NivelMaximo;
                    ws.Cell(row, 7).Value = item.NivelMinimo;
                    ws.Cell(row, 8).Value = item.NivelCritico;
                    ws.Cell(row, 9).Value = ObtenerEstadoTexto(item.NivelEstado);
                    ws.Cell(row, 10).Value = item.Proveedor;
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(ruta);
            });

            await MostrarNotificacionAsync("Exportacion exitosa", $"Se exportaron {datos.Count} existencias.");
        }
        catch (Exception ex)
        {
            await MostrarNotificacionAsync("Error al exportar", ex.Message);
        }
    }

    [RelayCommand]
    private Task ExportarPdfAsync() => Task.CompletedTask;

    private Task IrPaginaAsync(int page)
    {
        var target = Math.Clamp(page, 1, PageCount);
        if (target == Page) return Task.CompletedTask;
        Page = target;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task PagAnteriorAsync()
    {
        if (!HasPrev) return Task.CompletedTask;
        return IrPaginaAsync(Page - 1);
    }

    [RelayCommand]
    private Task PagSiguienteAsync()
    {
        if (!HasNext) return Task.CompletedTask;
        return IrPaginaAsync(Page + 1);
    }

    partial void OnPageChanged(int value)
    {
        if (!_inited) return;
        _ = CargarAsync();
    }

    partial void OnPageSizeChanged(int value)
    {
        if (value <= 0) PageSize = 30;
        if (!_inited) return;
        Page = 1;
        _ = CargarAsync();
    }

    private ExistenciaFiltro CrearFiltro(bool includePaging)
    {
        var filtro = new ExistenciaFiltro
        {
            Texto = string.IsNullOrWhiteSpace(Texto) ? null : Texto.Trim(),
            Nivel = NivelSeleccionado?.Estado
        };

        if (includePaging)
        {
            filtro.Page = Page;
            filtro.PageSize = PageSize;
        }

        return filtro;
    }

    private async Task CargarAsync(bool resetPage = false)
    {
        if (resetPage) Page = 1;
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var filtro = CrearFiltro(includePaging: true);

            var result = await _svc.BuscarAsync(filtro);

            Items.Clear();
            foreach (var item in result.Items)
            {
                Items.Add(item);
            }

            Total = result.Total;
            if (Page > PageCount)
            {
                Page = PageCount;
            }
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

    private void CerrarFormulario()
    {
        FormVm = null;
        IsInForm = false;
    }

    private static string ObtenerEstadoTexto(ExistenciaNivelEstado estado) => estado switch
    {
        ExistenciaNivelEstado.Critico => "Critico",
        ExistenciaNivelEstado.Bajo => "Bajo",
        ExistenciaNivelEstado.Seguro => "Seguro",
        ExistenciaNivelEstado.Normal => "Normal",
        ExistenciaNivelEstado.Excedido => "Excedido",
        _ => "Desconocido"
    };

    private async Task MostrarNotificacionAsync(string titulo, string mensaje)
    {
        NotifyTitle = titulo;
        NotifyMessage = mensaje;
        IsNotifyOpen = true;
        await Task.CompletedTask;
    }

    public sealed record NivelOption(string Texto, ExistenciaNivelEstado? Estado)
    {
        public override string ToString() => Texto;
    }
}

