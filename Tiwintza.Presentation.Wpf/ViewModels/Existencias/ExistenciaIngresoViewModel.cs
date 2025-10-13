using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Dtos;
using Tiwintza.Infrastructure.Dtos.Existencias;
using Tiwintza.Infrastructure.Services;

namespace Tiwintza.Presentation.Wpf.ViewModels.Existencias;

public sealed partial class ExistenciaIngresoViewModel : ObservableValidator
{
    private readonly IExistenciasCrudService _crud;

    public ExistenciaIngresoViewModel(IExistenciasCrudService crud)
    {
        _crud = crud;
        Fecha = DateTime.Today;

        Detalles.CollectionChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(PuedeGuardar));
        };

        ErrorsChanged += (_, __) => OnPropertyChanged(nameof(PuedeGuardar));
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsBusy))
            {
                OnPropertyChanged(nameof(PuedeGuardar));
            }
            else if (e.PropertyName == nameof(ProveedorId))
            {
                ValidateProperty(ProveedorId, nameof(ProveedorId));
            }
            else if (e.PropertyName == nameof(Fecha))
            {
                ValidateProperty(Fecha, nameof(Fecha));
            }
        };
    }

    public ObservableCollection<ProveedorDto> Proveedores { get; } = new();
    public ObservableCollection<ExistenciaComboItemDto> Productos { get; } = new();
    public ObservableCollection<IngresoDetalleItem> Detalles { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    [ObservableProperty, NotifyDataErrorInfo]
    [Required(ErrorMessage = "Seleccione un proveedor")]
    private long? proveedorId;

    [ObservableProperty] private string? numeroFactura;

    [ObservableProperty, NotifyDataErrorInfo]
    [Required(ErrorMessage = "Seleccione una fecha")]
    private DateTime? fecha;

    [ObservableProperty] private ExistenciaComboItemDto? productoSeleccionado;
    [ObservableProperty] private int productoCantidad = 1;
    [ObservableProperty] private decimal productoCostoUnitario;

    [ObservableProperty] private bool isProveedorQuickAddVisible;
    [ObservableProperty] private bool isProveedorQuickAddBusy;
    [ObservableProperty] private string? proveedorQuickAddError;
    [ObservableProperty] private string? nuevoProveedorRuc;
    [ObservableProperty] private string? nuevoProveedorRazonSocial;
    [ObservableProperty] private string? nuevoProveedorContacto;
    [ObservableProperty] private string? nuevoProveedorTelefono;
    [ObservableProperty] private string? nuevoProveedorEmail;

    [ObservableProperty] private bool isProductoQuickAddVisible;
    [ObservableProperty] private bool isProductoQuickAddBusy;
    [ObservableProperty] private string? productoQuickAddError;
    [ObservableProperty] private string? nuevoProductoCodigo;
    [ObservableProperty] private string? nuevoProductoNombre;
    [ObservableProperty] private string? nuevoProductoUnidad;
    [ObservableProperty] private string? nuevoProductoDescripcion;
    [ObservableProperty] private int? nuevoProductoNivelMaximo;
    [ObservableProperty] private int? nuevoProductoNivelSeguridad;
    [ObservableProperty] private int? nuevoProductoNivelMinimo;
    [ObservableProperty] private int? nuevoProductoNivelCritico;

    public decimal Subtotal => Detalles.Sum(d => d.Total);
    public decimal Total => Subtotal;

    public bool PuedeGuardar => !IsBusy && !HasErrors && ProveedorId is not null && Detalles.Count > 0;
    public bool PuedeGuardarProveedor => !IsProveedorQuickAddBusy
                                         && !string.IsNullOrWhiteSpace(NuevoProveedorRuc)
                                         && !string.IsNullOrWhiteSpace(NuevoProveedorRazonSocial);
    public bool PuedeGuardarProducto => !IsProductoQuickAddBusy
                                        && !string.IsNullOrWhiteSpace(NuevoProductoCodigo)
                                        && !string.IsNullOrWhiteSpace(NuevoProductoNombre)
                                        && !string.IsNullOrWhiteSpace(NuevoProductoUnidad)
                                        && NuevoProductoNivelMaximo is not null
                                        && NuevoProductoNivelSeguridad is not null
                                        && NuevoProductoNivelMinimo is not null
                                        && NuevoProductoNivelCritico is not null;

    public event Action<long>? Guardado;
    public event Action? Cancelado;
    public event Action? VolverSolicitado;

    public async Task InicializarAsync()
    {
        await CargarProveedoresAsync();
        await CargarProductosAsync();
        Fecha ??= DateTime.Today;
    }

    [RelayCommand]
    private async Task CargarProveedoresAsync()
    {
        Proveedores.Clear();
        var data = await _crud.ObtenerProveedoresAsync();
        foreach (var item in data)
        {
            Proveedores.Add(item);
        }
    }

    [RelayCommand]
    private async Task CargarProductosAsync()
    {
        Productos.Clear();
        var data = await _crud.ObtenerProductosAsync();
        foreach (var item in data)
        {
            Productos.Add(item);
        }
    }

    [RelayCommand]
    private void AgregarDetalle()
    {
        ErrorMessage = null;

        if (ProductoSeleccionado is null)
        {
            ErrorMessage = "Seleccione un producto";
            return;
        }

        if (ProductoCantidad <= 0)
        {
            ErrorMessage = "La cantidad debe ser mayor a cero";
            return;
        }

        if (ProductoCostoUnitario < 0)
        {
            ErrorMessage = "El costo unitario no puede ser negativo";
            return;
        }

        var existente = Detalles.FirstOrDefault(d => d.ExistenciaId == ProductoSeleccionado.Id);
        if (existente is not null)
        {
            existente.Cantidad += ProductoCantidad;
            existente.CostoUnitario = ProductoCostoUnitario;
        }
        else
        {
            var detalle = new IngresoDetalleItem(ProductoSeleccionado.Id,
                                                 ProductoSeleccionado.Codigo,
                                                 ProductoSeleccionado.Nombre,
                                                 ProductoSeleccionado.Unidad,
                                                 ProductoCantidad,
                                                 ProductoCostoUnitario);
            detalle.PropertyChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(Total));
            };
            Detalles.Add(detalle);
        }

        ProductoSeleccionado = null;
        ProductoCantidad = 1;
        ProductoCostoUnitario = 0;
        OnPropertyChanged(nameof(PuedeGuardar));
    }

    [RelayCommand]
    private void EliminarDetalle(IngresoDetalleItem? item)
    {
        if (item is null) return;
        Detalles.Remove(item);
        OnPropertyChanged(nameof(PuedeGuardar));
    }

    [RelayCommand]
    private void Cancelar()
    {
        Cancelado?.Invoke();
    }

    [RelayCommand]
    private void Volver()
    {
        VolverSolicitado?.Invoke();
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        ErrorMessage = null;
        ValidateAllProperties();
        OnPropertyChanged(nameof(PuedeGuardar));
        if (HasErrors)
        {
            ErrorMessage = "Revise los campos obligatorios";
            return;
        }

        if (Detalles.Count == 0)
        {
            ErrorMessage = "Debe agregar al menos un ítem";
            return;
        }

        try
        {
            IsBusy = true;
            var dto = new ExistenciaIngresoCreateDto
            {
                ProveedorId = ProveedorId!.Value,
                Fecha = DateOnly.FromDateTime(Fecha ?? DateTime.Today),
                NumeroFactura = string.IsNullOrWhiteSpace(NumeroFactura) ? null : NumeroFactura.Trim(),
                Detalles = Detalles
                    .Select(d => new ExistenciaIngresoDetalleDto
                    {
                        ExistenciaId = d.ExistenciaId,
                        Cantidad = d.Cantidad,
                        CostoUnitario = d.CostoUnitario
                    })
                    .ToList()
            };

            var compraId = await _crud.RegistrarIngresoAsync(dto);
            Guardado?.Invoke(compraId);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(PuedeGuardar));
        }
    }

    [RelayCommand]
    private void MostrarProveedorRapido()
    {
        LimpiarProveedorQuickAdd();
        IsProveedorQuickAddVisible = true;
    }

    [RelayCommand]
    private void MostrarProductoRapido()
    {
        LimpiarProductoQuickAdd();
        IsProductoQuickAddVisible = true;
    }

    [RelayCommand]
    private async Task GuardarProveedorRapidoAsync()
    {
        if (!PuedeGuardarProveedor) return;
        ProveedorQuickAddError = null;

        try
        {
            IsProveedorQuickAddBusy = true;
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
        catch (Exception ex)
        {
            ProveedorQuickAddError = ex.Message;
        }
        finally
        {
            IsProveedorQuickAddBusy = false;
        }
    }

    [RelayCommand]
    private void CancelarProveedorRapido()
    {
        LimpiarProveedorQuickAdd();
        IsProveedorQuickAddVisible = false;
    }

    [RelayCommand]
    private async Task GuardarProductoRapidoAsync()
    {
        if (!PuedeGuardarProducto)
        {
            ProductoQuickAddError = "Complete todos los campos";
            return;
        }

        if (!ValidarNivelesProducto(out var error))
        {
            ProductoQuickAddError = error;
            return;
        }

        try
        {
            IsProductoQuickAddBusy = true;
            ProductoQuickAddError = null;

            var dto = new ExistenciaQuickCreateDto
            {
                Codigo = NuevoProductoCodigo!.Trim(),
                Nombre = NuevoProductoNombre!.Trim(),
                Unidad = NuevoProductoUnidad!.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(NuevoProductoDescripcion) ? null : NuevoProductoDescripcion.Trim(),
                NivelMaximo = NuevoProductoNivelMaximo!.Value,
                NivelSeguridad = NuevoProductoNivelSeguridad!.Value,
                NivelMinimo = NuevoProductoNivelMinimo!.Value,
                NivelCritico = NuevoProductoNivelCritico!.Value,
                ProveedorPreferidoId = ProveedorId
            };

            var producto = await _crud.CrearExistenciaRapidaAsync(dto);
            if (Productos.All(p => p.Id != producto.Id))
            {
                Productos.Add(producto);
            }
            ProductoSeleccionado = producto;
            LimpiarProductoQuickAdd();
            IsProductoQuickAddVisible = false;
        }
        catch (Exception ex)
        {
            ProductoQuickAddError = ex.Message;
        }
        finally
        {
            IsProductoQuickAddBusy = false;
        }
    }

    [RelayCommand]
    private void CancelarProductoRapido()
    {
        LimpiarProductoQuickAdd();
        IsProductoQuickAddVisible = false;
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

    private void LimpiarProductoQuickAdd()
    {
        NuevoProductoCodigo = null;
        NuevoProductoNombre = null;
        NuevoProductoUnidad = null;
        NuevoProductoDescripcion = null;
        NuevoProductoNivelMaximo = null;
        NuevoProductoNivelSeguridad = null;
        NuevoProductoNivelMinimo = null;
        NuevoProductoNivelCritico = null;
        ProductoQuickAddError = null;
    }

    private bool ValidarNivelesProducto(out string? error)
    {
        var max = NuevoProductoNivelMaximo!.Value;
        var seg = NuevoProductoNivelSeguridad!.Value;
        var min = NuevoProductoNivelMinimo!.Value;
        var cri = NuevoProductoNivelCritico!.Value;

        if (cri > min)
        {
            error = "El nivel critico no puede ser mayor al minimo";
            return false;
        }

        if (min > seg)
        {
            error = "El nivel minimo no puede ser mayor al nivel de seguridad";
            return false;
        }

        if (seg > max)
        {
            error = "El nivel de seguridad no puede ser mayor al maximo";
            return false;
        }

        if (max <= 0)
        {
            error = "El nivel maximo debe ser mayor a cero";
            return false;
        }

        error = null;
        return true;
    }

    partial void OnIsProveedorQuickAddBusyChanged(bool value) => OnPropertyChanged(nameof(PuedeGuardarProveedor));
    partial void OnNuevoProveedorRucChanged(string? value) => OnPropertyChanged(nameof(PuedeGuardarProveedor));
    partial void OnNuevoProveedorRazonSocialChanged(string? value) => OnPropertyChanged(nameof(PuedeGuardarProveedor));

    partial void OnIsProductoQuickAddBusyChanged(bool value) => OnPropertyChanged(nameof(PuedeGuardarProducto));
    partial void OnNuevoProductoCodigoChanged(string? value) => OnPropertyChanged(nameof(PuedeGuardarProducto));
    partial void OnNuevoProductoNombreChanged(string? value) => OnPropertyChanged(nameof(PuedeGuardarProducto));
    partial void OnNuevoProductoUnidadChanged(string? value) => OnPropertyChanged(nameof(PuedeGuardarProducto));
    partial void OnNuevoProductoNivelMaximoChanged(int? value) => OnPropertyChanged(nameof(PuedeGuardarProducto));
    partial void OnNuevoProductoNivelSeguridadChanged(int? value) => OnPropertyChanged(nameof(PuedeGuardarProducto));
    partial void OnNuevoProductoNivelMinimoChanged(int? value) => OnPropertyChanged(nameof(PuedeGuardarProducto));
    partial void OnNuevoProductoNivelCriticoChanged(int? value) => OnPropertyChanged(nameof(PuedeGuardarProducto));

    public sealed partial class IngresoDetalleItem : ObservableObject
    {
        public IngresoDetalleItem(long existenciaId,
                                   string codigo,
                                   string nombre,
                                   string unidad,
                                   int cantidad,
                                   decimal costoUnitario)
        {
            ExistenciaId = existenciaId;
            Codigo = codigo;
            Nombre = nombre;
            Unidad = unidad;
            Cantidad = cantidad;
            CostoUnitario = costoUnitario;
        }

        public long ExistenciaId { get; }
        public string Codigo { get; }
        public string Nombre { get; }
        public string Unidad { get; }

        [ObservableProperty] private int cantidad;
        [ObservableProperty] private decimal costoUnitario;

        public decimal Total => Cantidad * CostoUnitario;

        partial void OnCantidadChanged(int value) => OnPropertyChanged(nameof(Total));
        partial void OnCostoUnitarioChanged(decimal value) => OnPropertyChanged(nameof(Total));
    }
}