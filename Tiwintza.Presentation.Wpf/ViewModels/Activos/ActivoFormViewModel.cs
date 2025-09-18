using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
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

    public string VidaUtilEnAniosHint => VidaUtilMeses is > 0 ? $"≈ {VidaUtilMeses / 12.0:0.0} años" : "Tiempo estimado de vida útil";
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
        PropertyChanged += (_, __) => OnPropertyChanged(nameof(PuedeGuardar));
    }

    // ===== Inicialización =====
    public async Task InicializarCrearAsync()
    {
        EsEdicion = false;
        await CargarCatalogosAsync();

        // estado por defecto "Bueno" si existe
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
        VidaUtilMeses = e.VidaUtilMeses;
        DepreciacionMensual = e.DepreciacionMensual;
        GarantiaMeses = e.GarantiaMeses;
        Observaciones = e.Observaciones;
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

        var fecCompra = FechaCompraDateTime.HasValue ? DateOnly.FromDateTime(FechaCompraDateTime.Value.Date) : (DateOnly?)null;

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
                    VidaUtilMeses = VidaUtilMeses,
                    DepreciacionMensual = DepreciacionMensual,
                    GarantiaMeses = GarantiaMeses,
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
                    VidaUtilMeses = VidaUtilMeses,
                    DepreciacionMensual = DepreciacionMensual,
                    GarantiaMeses = GarantiaMeses,
                    Observaciones = Observaciones
                };

                var id = await _crud.CrearAsync(dto);
                Guardado?.Invoke(id);
            }
        }

        catch (DuplicateCodeException)
        {
            // Limpia errores previos de la propiedad
            ClearErrors(nameof(CodigoInventario));

            // Agrega el error usando ValidationResult (no string plano)
            SetErrors(nameof(CodigoInventario), new[]
            {
            new ValidationResult("El código ya existe. Ingrese uno diferente.", new[] { nameof(CodigoInventario) })
            });

            // Notifica a la vista
            OnPropertyChanged(nameof(CodigoInventario));
            OnPropertyChanged(nameof(PuedeGuardar));
        }
    }

    partial void OnVidaUtilMesesChanged(int? value) => OnPropertyChanged(nameof(VidaUtilEnAniosHint));
    partial void OnDepreciacionMensualChanged(decimal? value) => OnPropertyChanged(nameof(DepreciacionAnualHint));
    partial void OnValorUnitarioChanged(decimal value) => OnPropertyChanged(nameof(DepreciacionAnualHint));
}
