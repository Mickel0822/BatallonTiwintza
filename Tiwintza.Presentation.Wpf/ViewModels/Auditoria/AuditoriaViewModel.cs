using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Dtos.Auditoria;
using Tiwintza.Infrastructure.Services;

namespace Tiwintza.Presentation.Wpf.ViewModels.Auditoria;

public sealed partial class AuditoriaViewModel : ObservableObject
{
    private readonly IAuditoriaService _service;
    private bool _initialized;

    public AuditoriaViewModel(IAuditoriaService service)
    {
        _service = service;
        Items = new ObservableCollection<AuditGroupItemViewModel>();
        Usuarios = new ObservableCollection<SelectOption>();
        Entidades = new ObservableCollection<SelectOption>();
        Acciones = new ObservableCollection<SelectOption>();

        PageSize = 25;
        Page = 1;
        SortBy = "fecha";
    }

    public ObservableCollection<AuditGroupItemViewModel> Items { get; }
    public ObservableCollection<SelectOption> Usuarios { get; }
    public ObservableCollection<SelectOption> Entidades { get; }
    public ObservableCollection<SelectOption> Acciones { get; }

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    [ObservableProperty] private DateTime? fechaInicio;
    [ObservableProperty] private DateTime? fechaFin;
    [ObservableProperty] private SelectOption? usuarioSeleccionado;
    [ObservableProperty] private SelectOption? entidadSeleccionada;
    [ObservableProperty] private SelectOption? accionSeleccionada;
    [ObservableProperty] private string? texto;

    [ObservableProperty] private string sortBy;
    [ObservableProperty] private bool sortDesc = true;

    [ObservableProperty] private int page;
    [ObservableProperty] private int pageSize;
    [ObservableProperty] private int total;

    [ObservableProperty] private AuditoriaItemModel? seleccionado;
    [ObservableProperty] private string? detalleFormateado;

    [ObservableProperty] private bool filtrosExpandido;

    [ObservableProperty] private bool isNotifyOpen;
    [ObservableProperty] private string? notifyTitle;
    [ObservableProperty] private string? notifyMessage;
    [ObservableProperty] private bool notifyIsError;

    public int PageCount => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling((double)Total / PageSize));

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await CargarCatalogosAsync();
        await BuscarAsync();
        _initialized = true;
    }

    [RelayCommand]
    private async Task BuscarAsync()
    {
        Page = 1;
        await CargarAsync();
    }

    [RelayCommand]
    private async Task RecargarAsync()
    {
        await CargarAsync();
    }

    [RelayCommand]
    private void LimpiarFiltros()
    {
        FechaInicio = null;
        FechaFin = null;
        UsuarioSeleccionado = Usuarios.FirstOrDefault();
        EntidadSeleccionada = Entidades.FirstOrDefault();
        AccionSeleccionada = Acciones.FirstOrDefault();
        Texto = null;
    }

    [RelayCommand]
    private void AlternarFiltros()
    {
        FiltrosExpandido = !FiltrosExpandido;
    }

    [RelayCommand]
    private void CerrarNotificacion() => IsNotifyOpen = false;

    [RelayCommand]
    private async Task PaginaAnteriorAsync()
    {
        if (Page <= 1) return;
        Page--;
        await CargarAsync();
    }

    [RelayCommand]
    private async Task PaginaSiguienteAsync()
    {
        if (Page >= PageCount) return;
        Page++;
        await CargarAsync();
    }

    [RelayCommand]
    private async Task CambiarOrdenAsync(string? columna)
    {
        if (string.IsNullOrWhiteSpace(columna)) return;
        columna = columna.Trim().ToLowerInvariant();
        if (SortBy == columna)
        {
            SortDesc = !SortDesc;
        }
        else
        {
            SortBy = columna;
            SortDesc = columna != "fecha" ? false : true;
        }

        await CargarAsync();
    }

    [RelayCommand]
    private async Task ExportarAsync()
    {
        try
        {
            ErrorMessage = null;
            IsBusy = true;
            var filtro = CrearFiltro(includePaging: false);
            var datos = await _service.ExportarAsync(filtro);
            if (datos.Count == 0)
            {
                ErrorMessage = "No hay registros para exportar";
                MostrarNotificacion("Exportar auditoria", "No hay registros para exportar.", true);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Exportar auditoria",
                Filter = "Libro de Excel (*.xlsx)|*.xlsx",
                FileName = $"Auditoria_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() != true) return;

            await Task.Run(() => ExportarExcel(dialog.FileName, datos));

            MostrarNotificacion("Exportar auditoria", $"El archivo se guardo en {dialog.FileName}.");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            MostrarNotificacion("Error al exportar", ex.Message, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Seleccionar(AuditoriaItemModel? item)
    {
        Seleccionado = item;
    }

    partial void OnSeleccionadoChanged(AuditoriaItemModel? value)
    {
        if (value?.DetalleJson is null)
        {
            DetalleFormateado = null;
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(value.DetalleJson);
            var options = new JsonSerializerOptions { WriteIndented = true };
            DetalleFormateado = JsonSerializer.Serialize(doc.RootElement, options);
        }
        catch (JsonException)
        {
            DetalleFormateado = value.DetalleJson;
        }
    }

    private async Task CargarCatalogosAsync()
    {
        try
        {
            IsBusy = true;
            var catalogos = await _service.ObtenerCatalogosAsync();

            Usuarios.Clear();
            Usuarios.Add(SelectOption.Todos("Todos los usuarios"));
            foreach (var u in catalogos.Usuarios)
            {
                Usuarios.Add(new SelectOption(u, u));
            }
            UsuarioSeleccionado = Usuarios.FirstOrDefault();

            Entidades.Clear();
            Entidades.Add(SelectOption.Todos("Todas las entidades"));
            foreach (var e in catalogos.Entidades)
            {
                Entidades.Add(new SelectOption(e, e));
            }
            EntidadSeleccionada = Entidades.FirstOrDefault();

            Acciones.Clear();
            Acciones.Add(SelectOption.Todos("Todas las acciones"));
            foreach (var a in catalogos.Acciones)
            {
                Acciones.Add(new SelectOption(a, a));
            }
            AccionSeleccionada = Acciones.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            MostrarNotificacion("Error al cargar catalogos", ex.Message, true);
        }

        finally
        {
            IsBusy = false;
        }
    }

    private async Task CargarAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var filtro = CrearFiltro(includePaging: true);
            var resultado = await _service.BuscarAgrupadasAsync(filtro);

            Items.Clear();
            foreach (var item in resultado.Items)
            {
                Items.Add(new AuditGroupItemViewModel(item));
            }

            Total = resultado.Total;
            PageSize = resultado.PageSize;
            Page = resultado.Page;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            MostrarNotificacion("Error al cargar auditoria", ex.Message, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private AuditoriaFiltroDto CrearFiltro(bool includePaging)
    {
        var page = includePaging ? (Page <= 0 ? 1 : Page) : 1;
        var size = includePaging ? (PageSize <= 0 ? 25 : PageSize) : 2000;

        return new AuditoriaFiltroDto
        {
            FechaInicio = FechaInicio?.Date,
            FechaFin = FechaFin.HasValue ? FechaFin.Value.Date.AddDays(1).AddTicks(-1) : null,
            Usuario = UsuarioSeleccionado?.Value,
            Entidad = EntidadSeleccionada?.Value,
            Accion = AccionSeleccionada?.Value,
            Texto = string.IsNullOrWhiteSpace(Texto) ? null : Texto.Trim(),
            SortBy = SortBy,
            SortDesc = SortDesc,
            Page = page,
            PageSize = size
        };
    }

    private void MostrarNotificacion(string titulo, string mensaje, bool esError = false)
    {
        NotifyTitle = titulo;
        NotifyMessage = mensaje;
        NotifyIsError = esError;
        IsNotifyOpen = true;
    }

    private static void ExportarExcel(string ruta, IReadOnlyList<AuditoriaListItemDto> datos)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Auditoria");

        var headers = new[] { "Fecha/Hora", "Usuario", "Accion", "Entidad", "ID", "Resumen" };
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
            ws.Cell(row, 1).Value = item.FechaHora;
            ws.Cell(row, 1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
            ws.Cell(row, 2).Value = item.Usuario;
            ws.Cell(row, 3).Value = item.Accion;
            ws.Cell(row, 4).Value = item.Entidad;
            ws.Cell(row, 5).Value = item.EntidadId;
            ws.Cell(row, 6).Value = item.Resumen;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(ruta);
    }
}




