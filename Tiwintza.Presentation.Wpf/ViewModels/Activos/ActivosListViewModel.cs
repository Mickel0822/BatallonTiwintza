using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using System;

using System.Collections.ObjectModel;

using System.Threading.Tasks;

using Tiwintza.Infrastructure.Dtos;

using Microsoft.Win32;

using Tiwintza.Infrastructure.Services;

using ClosedXML.Excel;




namespace Tiwintza.Presentation.Wpf.ViewModels.Activos;



public partial class ActivosListViewModel : ObservableObject

{

    private readonly IActivosService _svc;

    private readonly IActivosCrudService _crud;



    private ActivoFormViewModel? _activoFormVm;

    private Action<long>? _activoFormGuardadoHandler;

    private Action? _activoFormCanceladoHandler;

    private Action? _activoFormVolverHandler;



    private ActivoTrasladoViewModel? _trasladoVm;

    private Action? _trasladoVolverHandler;

    private Action? _trasladoTrasladarHandler;



    private ActivoBajaViewModel? _bajaVm;

    private Action? _bajaVolverHandler;

    private Action? _bajaBajaHandler;



    private object? formVm;



    public ActivosListViewModel(IActivosService svc, IActivosCrudService crud)

    {

        _svc = svc;

        _crud = crud;

    }



    [ObservableProperty] private bool isInForm;

    [ObservableProperty] private bool isActionDialogOpen;

    [ObservableProperty] private ActivoListItemDto? accionSeleccionada;



    [ObservableProperty] private string? texto;

    [ObservableProperty] private long? areaId;

    [ObservableProperty] private long? estadoId;

    [ObservableProperty] private long? tipoId;

    

    [ObservableProperty] private int page = 1;

    [ObservableProperty] private int pageSize = 30;



    [ObservableProperty] private int total;

    [ObservableProperty] private int totalOperativos;

    [ObservableProperty] private int totalBaja;



    [ObservableProperty] private int gridTotal;



    public int PageCount => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling((double)GridTotal / PageSize));

    public bool HasPrev => Page > 1;

    public bool HasNext => Page < PageCount;



    public ObservableCollection<ActivoListItemDto> Items { get; } = new();



    public ObservableCollection<IdNombreDto> Areas { get; } = new();

    public ObservableCollection<IdNombreDto> Estados { get; } = new();

    public ObservableCollection<IdNombreDto> Tipos { get; } = new();



    public object? FormVm

    {

        get => formVm;

        private set => SetFormVm(value);

    }
    // ===== Notificaciones =====
    [ObservableProperty] private bool isNotifyOpen;
    [ObservableProperty] private string? notifyTitle;
    [ObservableProperty] private string? notifyMessage;

    [RelayCommand]
    private void CerrarNotificacion() => IsNotifyOpen = false;

    private Task MostrarNotificacionAsync(string title, string message)
    {
        NotifyTitle = title;
        NotifyMessage = message;
        IsNotifyOpen = true;
        return Task.CompletedTask;
    }




    private void SetFormVm(object? value)

    {

        if (ReferenceEquals(formVm, value)) return;



        var oldVm = formVm;

        if (oldVm is not null)

        {

            DetachFormVm(oldVm);

        }



        OnPropertyChanging(nameof(FormVm));

        formVm = value;

        OnPropertyChanged(nameof(FormVm));



        if (value is not null)

        {

            AttachFormVm(value);

        }

    }



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

    private ActivoFiltro CrearFiltro() => new()



    {



        Texto = Texto,



        AreaId = AreaId,



        EstadoId = EstadoId,



        TipoId = TipoId,






        Page = Page,



        PageSize = PageSize



    };







    [RelayCommand]



    public async Task CargarAsync()



    {



        var filtro = CrearFiltro();



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



    [RelayCommand]

    public async Task NuevoAsync()

    {

        CerrarDialogoAcciones();

        var form = new ActivoFormViewModel(_crud);

        FormVm = form;

        IsInForm = true;

        await form.InicializarCrearAsync();

    }



    [RelayCommand]

    public async Task EditarAsync(long id)

    {

        CerrarDialogoAcciones();

        var form = new ActivoFormViewModel(_crud);

        FormVm = form;

        IsInForm = true;

        await form.InicializarEditarAsync(id);

    }



    [RelayCommand]

    private void MostrarAcciones(ActivoListItemDto item)

    {

        if (IsInForm || IsActionDialogOpen) return;

        AccionSeleccionada = item;

        IsActionDialogOpen = true;

    }



    [RelayCommand]

    private void CancelarAcciones() => CerrarDialogoAcciones();



    [RelayCommand]

    private async Task EditarSeleccionadoAsync()

    {

        if (AccionSeleccionada is null) return;

        var id = AccionSeleccionada.Id;

        CerrarDialogoAcciones();

        await EditarAsync(id);

    }



    [RelayCommand]
    private async Task AbrirTrasladoAsync()
    {
        if (AccionSeleccionada is null) return;
        var trasladoVm = new ActivoTrasladoViewModel(_crud);
        trasladoVm.Initialize(AccionSeleccionada);
        FormVm = trasladoVm;
        IsInForm = true;
        CerrarDialogoAcciones();
        await trasladoVm.InicializarAsync();
    }


    [RelayCommand]
    private async Task AbrirBajaAsync()
    {
        if (AccionSeleccionada is null) return;
        var bajaVm = new ActivoBajaViewModel(_crud);
        bajaVm.Initialize(AccionSeleccionada);
        FormVm = bajaVm;
        IsInForm = true;
        CerrarDialogoAcciones();
        await bajaVm.InicializarAsync();
    }


    private void AttachFormVm(object? vm)

    {

        switch (vm)

        {

            case ActivoFormViewModel form:

                _activoFormVm = form;

                _activoFormGuardadoHandler = async _ =>

                {

                    CerrarFormulario();

                    await CargarAsync(); await MostrarNotificacionAsync("Operación éxitosa", "El activo fue guardado con éxito.");

                };

                _activoFormCanceladoHandler = CerrarFormulario;

                _activoFormVolverHandler = CerrarFormulario;

                form.Guardado += _activoFormGuardadoHandler;

                form.Cancelado += _activoFormCanceladoHandler;

                form.VolverSolicitado += _activoFormVolverHandler;

                break;



            case ActivoTrasladoViewModel traslado:

                _trasladoVm = traslado;

                _trasladoVolverHandler = CerrarFormulario;

                _trasladoTrasladarHandler = async () => { CerrarFormulario(); await CargarAsync(); await MostrarNotificacionAsync("Traslado completado", "El activo fue trasladado con éxito."); };

                traslado.VolverSolicitado += _trasladoVolverHandler;

                traslado.TrasladarSolicitado += _trasladoTrasladarHandler;

                break;



            case ActivoBajaViewModel baja:

                _bajaVm = baja;

                _bajaVolverHandler = CerrarFormulario;

                _bajaBajaHandler = async () => { CerrarFormulario(); await CargarAsync(); await MostrarNotificacionAsync("Baja registrada", "El activo fue dado de baja con éxito."); };

                baja.VolverSolicitado += _bajaVolverHandler;

                baja.DarBajaSolicitado += _bajaBajaHandler;

                break;

        }

    }



    private void DetachFormVm(object? vm)

    {

        switch (vm)

        {

            case ActivoFormViewModel form when _activoFormVm == form:

                if (_activoFormGuardadoHandler is not null) form.Guardado -= _activoFormGuardadoHandler;

                if (_activoFormCanceladoHandler is not null) form.Cancelado -= _activoFormCanceladoHandler;

                if (_activoFormVolverHandler is not null) form.VolverSolicitado -= _activoFormVolverHandler;

                _activoFormVm = null;

                _activoFormGuardadoHandler = null;

                _activoFormCanceladoHandler = null;

                _activoFormVolverHandler = null;

                break;



            case ActivoTrasladoViewModel traslado when _trasladoVm == traslado:

                if (_trasladoVolverHandler is not null) traslado.VolverSolicitado -= _trasladoVolverHandler;

                if (_trasladoTrasladarHandler is not null) traslado.TrasladarSolicitado -= _trasladoTrasladarHandler;

                _trasladoVm = null;

                _trasladoVolverHandler = null;

                _trasladoTrasladarHandler = null;

                break;



            case ActivoBajaViewModel baja when _bajaVm == baja:

                if (_bajaVolverHandler is not null) baja.VolverSolicitado -= _bajaVolverHandler;

                if (_bajaBajaHandler is not null) baja.DarBajaSolicitado -= _bajaBajaHandler;

                _bajaVm = null;

                _bajaVolverHandler = null;

                _bajaBajaHandler = null;

                break;

        }

    }



    private void CerrarFormulario()

    {

        if (formVm is null) return;

        IsInForm = false;

        FormVm = null;

    }



    private void CerrarDialogoAcciones()

    {

        IsActionDialogOpen = false;

        AccionSeleccionada = null;

    }



    partial void OnPageSizeChanged(int value) => _ = CargarAsync();



    [RelayCommand]
    public async Task ExportarExcelAsync()
    {
        var filtro = CrearFiltro();
        var datos = await _svc.ExportarMaestroAsync(filtro);

        if (datos.Count == 0)
        {
            await MostrarNotificacionAsync("Sin resultados", "No se encontraron activos para exportar con el filtro actual.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Exportar activos a Excel",
            Filter = "Libro de Excel (*.xlsx)|*.xlsx",
            FileName = $"Activos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
            AddExtension = true,
            DefaultExt = ".xlsx",
            OverwritePrompt = true
        };

        var result = dialog.ShowDialog();
        if (result != true) return;

        try
        {
            var ruta = dialog.FileName;

            await Task.Run(() =>
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Activos");

                var headers = new[]
                {
                    "Código","Nombre","Descripción","Marca","Modelo","Serie","Material","Tipo","Estado","En baja",
                    "Área","Valor unitario","Fecha compra","Proveedor","Vida útil (meses)","Depreciación mensual",
                    "Documento autorización","Garantía (meses)","Observaciones"
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
                    ws.Cell(row, 3).Value = item.Descripcion;
                    ws.Cell(row, 4).Value = item.Marca;
                    ws.Cell(row, 5).Value = item.Modelo;
                    ws.Cell(row, 6).Value = item.Serie;
                    ws.Cell(row, 7).Value = item.Material;
                    ws.Cell(row, 8).Value = item.Tipo;
                    ws.Cell(row, 9).Value = item.Estado;
                    ws.Cell(row, 10).Value = item.EnBaja ? "Sí" : "No";
                    ws.Cell(row, 11).Value = item.Area;
                    ws.Cell(row, 12).Value = item.ValorUnitario;
                    ws.Cell(row, 12).Style.NumberFormat.Format = "$ #,##0.00";

                    if (item.FechaCompra.HasValue)
                    {
                        ws.Cell(row, 13).Value = item.FechaCompra.Value.ToDateTime(TimeOnly.MinValue);
                        ws.Cell(row, 13).Style.NumberFormat.Format = "dd/MM/yyyy";
                    }

                    ws.Cell(row, 14).Value = item.Proveedor;
                    if (item.VidaUtilMeses.HasValue) ws.Cell(row, 15).Value = item.VidaUtilMeses.Value;

                    if (item.DepreciacionMensual.HasValue)
                    {
                        ws.Cell(row, 16).Value = item.DepreciacionMensual.Value;
                        ws.Cell(row, 16).Style.NumberFormat.Format = "$ #,##0.00";
                    }

                    ws.Cell(row, 17).Value = item.DocumentoAutorizacion ? "Sí" : "No";
                    if (item.GarantiaMeses.HasValue) ws.Cell(row, 18).Value = item.GarantiaMeses.Value;
                    ws.Cell(row, 19).Value = item.Observaciones;
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(ruta);
            });

            await MostrarNotificacionAsync("Exportación éxitosa", $"Se exportaron {datos.Count} activos.");
        }
        catch (Exception ex)
        {
            await MostrarNotificacionAsync("Error al exportar", ex.Message);
        }
    }

    [RelayCommand] public Task ExportarPdfAsync() => Task.CompletedTask;

}

