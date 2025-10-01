using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Dtos;
using Tiwintza.Infrastructure.Services;

namespace Tiwintza.Presentation.Wpf.ViewModels.Activos;

// IMPORTANTE: hereda de ObservableValidator (no ObservableObject)
public sealed partial class ActivoFormViewModel : ObservableValidator
{
    private readonly IActivosCrudService _crud;

    public bool EsEdicion { get; private set; }
    public long? Id { get; private set; }

    public string Titulo => EsEdicion ? "Editar Activo" : "Registrar Activo";

    // Catálogos
    public ObservableCollection<IdNombreDto> Tipos { get; } = new();
    public ObservableCollection<IdNombreDto> Estados { get; } = new();
    public ObservableCollection<IdNombreDto> Areas { get; } = new();
    public ObservableCollection<ProveedorDto> Proveedores { get; } = new();
    public ObservableCollection<ActivoMovimientoDto> Movimientos { get; } = new();
    [ObservableProperty] private string? codigoInventarioError;

    // ===== Campos =====
    [ObservableProperty, NotifyDataErrorInfo]
    [Required(ErrorMessage = "El código es obligatorio")]
    [MaxLength(50)]
    private string? codigoInventario;

    [ObservableProperty, NotifyDataErrorInfo]
    [Required(ErrorMessage = "La descripción es obligatoria")]
    private string? nombre;

    [ObservableProperty, NotifyDataErrorInfo]
    [Required] private long? tipoId;

    [ObservableProperty, NotifyDataErrorInfo]
    [Required] private long? estadoId;

    [ObservableProperty, NotifyDataErrorInfo]
    [Required] private long? areaId;

    [ObservableProperty, NotifyDataErrorInfo]
    [Range(0, 999999999, ErrorMessage = "Valor inválido")]
    private decimal valorUnitario;

    [ObservableProperty] private DateTime? fechaCompraDateTime;
    [ObservableProperty] private long? proveedorId;
    [ObservableProperty] private string? descripcion;
    [ObservableProperty] private string? marca;
    [ObservableProperty] private string? modelo;
    [ObservableProperty] private string? serie;
    [ObservableProperty] private string? material;
    [ObservableProperty] private string? observaciones;

    [ObservableProperty] private int? vidaUtilMeses;
    [ObservableProperty] private int? garantiaMeses;
    [ObservableProperty] private decimal? depreciacionMensual;
    [ObservableProperty] private bool documentoAutorizacion;
    [ObservableProperty] private bool isProveedorQuickAddVisible;
    [ObservableProperty] private bool isProveedorQuickAddBusy;
    [ObservableProperty] private string? proveedorQuickAddError;
    [ObservableProperty] private string? nuevoProveedorRuc;
    [ObservableProperty] private string? nuevoProveedorRazonSocial;
    [ObservableProperty] private string? nuevoProveedorContacto;
    [ObservableProperty] private string? nuevoProveedorTelefono;
    [ObservableProperty] private string? nuevoProveedorEmail;

    public bool DebeMostrarParametrosContables => DocumentoAutorizacion;
    public bool PuedeGuardarProveedor => !IsProveedorQuickAddBusy
                                         && !string.IsNullOrWhiteSpace(NuevoProveedorRuc)
                                         && !string.IsNullOrWhiteSpace(NuevoProveedorRazonSocial);

    public string VidaUtilEnAniosHint => VidaUtilMeses is > 0 ? $"~ {VidaUtilMeses / 12.0:0.0} años" : "Tiempo estimado de vida útil";
    public string DepreciacionAnualHint =>
        ValorUnitario <= 0 || (DepreciacionMensual ?? 0m) <= 0m
            ? "Porcentaje anual aproximado"
            : $"≈ {(double)((DepreciacionMensual!.Value * 12m) / (ValorUnitario == 0 ? 1 : ValorUnitario)) * 100.0:0.##}% anual";

    public bool PuedeGuardar => !HasErrors
                                && !string.IsNullOrWhiteSpace(CodigoInventario)
                                && !string.IsNullOrWhiteSpace(Nombre)
                                && TipoId is not null && EstadoId is not null && AreaId is not null;

    public event Action<long>? Guardado;
    public event Action? Cancelado;
    public event Action? VolverSolicitado;

    public ActivoFormViewModel(IActivosCrudService crud)
    {
        _crud = crud;

        ErrorsChanged += (_, __) => OnPropertyChanged(nameof(PuedeGuardar));
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(PuedeGuardar))
                OnPropertyChanged(nameof(PuedeGuardar));
        };
    }

    // ===== Inicialización =====
    public async Task InicializarCrearAsync()
    {
        EsEdicion = false;
        await CargarCatalogosAsync();

        DocumentoAutorizacion = false;
        Movimientos.Clear();
        LimpiarProveedorQuickAdd();
        IsProveedorQuickAddVisible = false;

        var bueno = Estados.FirstOrDefault(e => string.Equals(e.Nombre, "Bueno", StringComparison.OrdinalIgnoreCase));
        EstadoId ??= bueno?.Id;
        FechaCompraDateTime ??= DateTime.Today;
    }

    public async Task InicializarEditarAsync(long id)
    {
        EsEdicion = true;
        await CargarCatalogosAsync();

        var e = await _crud.ObtenerAsync(id);
        Id = e.Id;
        CodigoInventario = e.CodigoInventario;
        Nombre = e.Nombre;
        TipoId = e.TipoId;
        Descripcion = e.Descripcion;
        Marca = e.Marca;
        Modelo = e.Modelo;
        Serie = e.Serie;
        Material = e.Material;
        EstadoId = e.EstadoId;
        AreaId = e.AreaId;
        ValorUnitario = e.ValorUnitario;
        FechaCompraDateTime = e.FechaCompra is null ? null : e.FechaCompra.Value.ToDateTime(TimeOnly.MinValue);
        ProveedorId = e.ProveedorId;
        DocumentoAutorizacion = e.DocumentoAutorizacion;

        if (DocumentoAutorizacion)
        {
            VidaUtilMeses = e.VidaUtilMeses;
            DepreciacionMensual = e.DepreciacionMensual;
            GarantiaMeses = e.GarantiaMeses;
        }
        else
        {
            VidaUtilMeses = null;
            DepreciacionMensual = null;
            GarantiaMeses = null;
        }

        Observaciones = e.Observaciones;

        Movimientos.Clear();
        var movimientos = await _crud.MovimientosAsync(id);
        foreach (var movimiento in movimientos)
        {
            Movimientos.Add(movimiento);
        }
    }

    private async Task CargarCatalogosAsync()
    {
        Tipos.Clear(); Estados.Clear(); Areas.Clear(); Proveedores.Clear();

        var (areas, estados, tipos, proveedores) = await _crud.CatalogosFormAsync();
        foreach (var t in tipos) Tipos.Add(t);
        foreach (var e in estados) Estados.Add(e);
        foreach (var a in areas) Areas.Add(a);
        foreach (var p in proveedores) Proveedores.Add(p);
    }

    // ===== Acciones =====
    [RelayCommand] private void Volver() => VolverSolicitado?.Invoke();
    [RelayCommand] private void Cancelar() => Cancelado?.Invoke();

    [RelayCommand]
    private async Task GuardarAsync()
    {
        ValidateAllProperties();
        if (!PuedeGuardar) return;

        CodigoInventarioError = null;

        var fecCompra = FechaCompraDateTime.HasValue ? DateOnly.FromDateTime(FechaCompraDateTime.Value.Date) : (DateOnly?)null;
        var vidaUtil = DocumentoAutorizacion ? VidaUtilMeses : null;
        var depreciacion = DocumentoAutorizacion ? DepreciacionMensual : null;
        var garantia = DocumentoAutorizacion ? GarantiaMeses : null;

        try
        {
            if (EsEdicion && Id is not null)
            {
                var dto = new ActivoUpdateDto
                {
                    CodigoInventario = CodigoInventario!,
                    Nombre = Nombre!,
                    TipoId = TipoId!.Value,
                    Descripcion = Descripcion,
                    Marca = Marca,
                    Modelo = Modelo,
                    Serie = Serie,
                    Material = Material,
                    EstadoId = EstadoId!.Value,
                    AreaId = AreaId!.Value,
                    ValorUnitario = ValorUnitario,
                    FechaCompra = fecCompra,
                    ProveedorId = ProveedorId,
                    VidaUtilMeses = vidaUtil,
                    DepreciacionMensual = depreciacion,
                    DocumentoAutorizacion = DocumentoAutorizacion,
                    GarantiaMeses = garantia,
                    Observaciones = Observaciones
                };

                await _crud.ActualizarAsync(Id.Value, dto);
                Guardado?.Invoke(Id.Value);
            }
            else
            {
                var dto = new ActivoCreateDto
                {
                    CodigoInventario = CodigoInventario!,
                    Nombre = Nombre!,
                    TipoId = TipoId!.Value,
                    Descripcion = Descripcion,
                    Marca = Marca,
                    Modelo = Modelo,
                    Serie = Serie,
                    Material = Material,
                    EstadoId = EstadoId!.Value,
                    AreaId = AreaId!.Value,
                    ValorUnitario = ValorUnitario,
                    FechaCompra = fecCompra,
                    ProveedorId = ProveedorId,
                    VidaUtilMeses = vidaUtil,
                    DepreciacionMensual = depreciacion,
                    DocumentoAutorizacion = DocumentoAutorizacion,
                    GarantiaMeses = garantia,
                    Observaciones = Observaciones
                };

                var nuevoId = await _crud.CrearAsync(dto);
                Guardado?.Invoke(nuevoId);
            }
        }
        catch (DuplicateCodeException)
        {
            ClearErrors(nameof(CodigoInventario));
            CodigoInventarioError = "El código ya existe. Ingrese uno diferente.";
            OnPropertyChanged(nameof(CodigoInventario));
            OnPropertyChanged(nameof(PuedeGuardar));
        }
    }

    [RelayCommand]
    private void MostrarAgregarProveedor()
    {
        IsProveedorQuickAddVisible = true;
        ProveedorQuickAddError = null;
    }

    [RelayCommand]
    private void CancelarAgregarProveedor()
    {
        IsProveedorQuickAddVisible = false;
        LimpiarProveedorQuickAdd();
    }

    [RelayCommand]
    private async Task GuardarProveedorRapidoAsync()
    {
        if (IsProveedorQuickAddBusy) return;

        ProveedorQuickAddError = null;

        if (!ValidarProveedorQuickAdd(out var mensaje))
        {
            ProveedorQuickAddError = mensaje;
            return;
        }

        IsProveedorQuickAddBusy = true;

        try
        {
            var dto = new ProveedorCreateDto
            {
                Ruc = NuevoProveedorRuc!.Trim(),
                RazonSocial = NuevoProveedorRazonSocial!.Trim(),
                Contacto = string.IsNullOrWhiteSpace(NuevoProveedorContacto) ? null : NuevoProveedorContacto.Trim(),
                Telefono = string.IsNullOrWhiteSpace(NuevoProveedorTelefono) ? null : NuevoProveedorTelefono.Trim(),
                Email = string.IsNullOrWhiteSpace(NuevoProveedorEmail) ? null : NuevoProveedorEmail.Trim()
            };

            var proveedor = await _crud.CrearProveedorRapidoAsync(dto);

            if (Proveedores.All(p => p.Id != proveedor.Id))
            {
                Proveedores.Add(proveedor);
            }

            ProveedorId = proveedor.Id;
            LimpiarProveedorQuickAdd();
            IsProveedorQuickAddVisible = false;
        }
        catch (DuplicateCodeException)
        {
            ProveedorQuickAddError = "El RUC ya se encuentra registrado.";
        }
        catch (Exception ex)
        {
            ProveedorQuickAddError = ex.Message;
        }
        finally
        {
            IsProveedorQuickAddBusy = false;
        }
    }

    private bool ValidarProveedorQuickAdd(out string? mensaje)
    {
        if (string.IsNullOrWhiteSpace(NuevoProveedorRuc))
        {
            mensaje = "El RUC es obligatorio.";
            return false;
        }

        var ruc = NuevoProveedorRuc.Trim();
        if (ruc.Length != 13 || !ruc.All(char.IsDigit))
        {
            mensaje = "El RUC debe tener 13 dígitos.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(NuevoProveedorRazonSocial))
        {
            mensaje = "La razón social es obligatoria.";
            return false;
        }

        mensaje = null;
        return true;
    }

    private void LimpiarProveedorQuickAdd()
    {
        NuevoProveedorRuc = null;
        NuevoProveedorRazonSocial = null;
        NuevoProveedorContacto = null;
        NuevoProveedorTelefono = null;
        NuevoProveedorEmail = null;
        ProveedorQuickAddError = null;
    }

    private void RecalcularDepreciacionAutomatica()
    {
        if (!DocumentoAutorizacion) return;
        if (VidaUtilMeses is null || VidaUtilMeses <= 0) return;
        if (ValorUnitario <= 0) return;

        var calculada = Math.Round(ValorUnitario / VidaUtilMeses.Value, 2);
        if (!DepreciacionMensual.HasValue || Math.Abs(DepreciacionMensual.Value - calculada) > 0.01m)
        {
            DepreciacionMensual = calculada;
        }
    }

    partial void OnVidaUtilMesesChanged(int? value)
    {
        OnPropertyChanged(nameof(VidaUtilEnAniosHint));
        RecalcularDepreciacionAutomatica();
        OnPropertyChanged(nameof(DepreciacionAnualHint));
    }

    partial void OnDepreciacionMensualChanged(decimal? value)
    {
        OnPropertyChanged(nameof(DepreciacionAnualHint));
    }

    partial void OnValorUnitarioChanged(decimal value)
    {
        RecalcularDepreciacionAutomatica();
        OnPropertyChanged(nameof(DepreciacionAnualHint));
    }

    partial void OnDocumentoAutorizacionChanged(bool value)
    {
        OnPropertyChanged(nameof(DebeMostrarParametrosContables));
        OnPropertyChanged(nameof(DepreciacionAnualHint));

        if (!value)
        {
            VidaUtilMeses = null;
            DepreciacionMensual = null;
            GarantiaMeses = null;
        }
        else
        {
            RecalcularDepreciacionAutomatica();
        }
    }

    partial void OnNuevoProveedorRucChanged(string? value) => OnPropertyChanged(nameof(PuedeGuardarProveedor));
    partial void OnNuevoProveedorRazonSocialChanged(string? value) => OnPropertyChanged(nameof(PuedeGuardarProveedor));
    partial void OnIsProveedorQuickAddBusyChanged(bool value) => OnPropertyChanged(nameof(PuedeGuardarProveedor));
}

