using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Tiwintza.Infrastructure.Dtos.Catalogos;
using Tiwintza.Infrastructure.Services;
using Tiwintza.Infrastructure.Services.Auth;

namespace Tiwintza.Presentation.Wpf.ViewModels.Catalogos;

public sealed partial class CatalogosViewModel : ObservableObject
{
    private readonly ICatalogosService _service;
    private readonly IAuthService _auth;
    private bool _initialized;

    public CatalogosViewModel(ICatalogosService service, IAuthService auth)
    {
        _service = service;
        _auth = auth;
        Areas = new ObservableCollection<CatalogItemModel>();
        Tipos = new ObservableCollection<CatalogItemModel>();
        Proveedores = new ObservableCollection<ProveedorModel>();
        NuevoProveedor = new ProveedorFormModel();
    }

    public ObservableCollection<CatalogItemModel> Areas { get; }
    public ObservableCollection<CatalogItemModel> Tipos { get; }
    public ObservableCollection<ProveedorModel> Proveedores { get; }
    public bool IsAdmin => _auth.Current?.Roles.Any(r => r.Equals("admin", StringComparison.OrdinalIgnoreCase) || r.Equals("administrador", StringComparison.OrdinalIgnoreCase)) ?? false;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    [ObservableProperty] private string? nuevoAreaNombre;
    [ObservableProperty] private string? nuevoTipoNombre;

    [ObservableProperty] private bool isAreaDialogOpen;
    [ObservableProperty] private bool isTipoDialogOpen;
    [ObservableProperty] private CatalogItemModel? areaEdicion;
    [ObservableProperty] private CatalogItemModel? tipoEdicion;

    [ObservableProperty] private bool isProveedorDialogOpen;
    [ObservableProperty] private ProveedorFormModel nuevoProveedor;
    [ObservableProperty] private ProveedorFormModel? proveedorEdicion;

    [ObservableProperty] private bool isNotifyOpen;
    [ObservableProperty] private string? notifyTitle;
    [ObservableProperty] private string? notifyMessage;
    [ObservableProperty] private bool notifyIsError;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        OnPropertyChanged(nameof(IsAdmin));
        await CargarAsync();
        _initialized = true;
    }

    [RelayCommand]
    private void CerrarNotificacion() => IsNotifyOpen = false;

    [RelayCommand]
    private async Task RecargarAsync()
    {
        await CargarAsync();
    }

    [RelayCommand(CanExecute = nameof(IsAdmin))]
    private async Task AgregarAreaAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoAreaNombre))
        {
            MostrarNotificacion("Validacion", "Ingrese el nombre del area", true);
            return;
        }

        try
        {
            var dto = await _service.CrearAreaAsync(NuevoAreaNombre.Trim());
            Areas.Add(new CatalogItemModel(dto.Id, dto.Nombre));
            NuevoAreaNombre = null;
            MostrarNotificacion("Area creada", "El area se registro correctamente.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", ex.Message, true);
        }
    }

    [RelayCommand(CanExecute = nameof(IsAdmin))]
    private void AbrirEditarArea(CatalogItemModel? item)
    {
        if (item is null) return;
        AreaEdicion = item.Clone();
        IsAreaDialogOpen = true;
    }

    [RelayCommand]
    private async Task GuardarAreaAsync()
    {
        if (AreaEdicion is null) return;
        try
        {
            var dto = await _service.ActualizarAreaAsync(AreaEdicion.Id, AreaEdicion.Nombre.Trim());
            var original = Areas.First(a => a.Id == dto.Id);
            original.Nombre = dto.Nombre;
            IsAreaDialogOpen = false;
            AreaEdicion = null;
            MostrarNotificacion("Area actualizada", "Los cambios fueron guardados.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", ex.Message, true);
        }
    }

    [RelayCommand]
    private void CancelarEditarArea()
    {
        AreaEdicion = null;
        IsAreaDialogOpen = false;
    }

    [RelayCommand(CanExecute = nameof(IsAdmin))]
    private async Task EliminarAreaAsync(CatalogItemModel? item)
    {
        if (item is null) return;
        if (MessageBox.Show($"Eliminar el area {item.Nombre}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            await _service.EliminarAreaAsync(item.Id);
            Areas.Remove(item);
            MostrarNotificacion("Area eliminada", "El registro fue eliminado.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", ex.Message, true);
        }
    }

    [RelayCommand(CanExecute = nameof(IsAdmin))]
    private async Task AgregarTipoAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoTipoNombre))
        {
            MostrarNotificacion("Validacion", "Ingrese el nombre del tipo", true);
            return;
        }

        try
        {
            var dto = await _service.CrearTipoAsync(NuevoTipoNombre.Trim());
            Tipos.Add(new CatalogItemModel(dto.Id, dto.Nombre));
            NuevoTipoNombre = null;
            MostrarNotificacion("Tipo creado", "El tipo se registro correctamente.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", ex.Message, true);
        }
    }

    [RelayCommand(CanExecute = nameof(IsAdmin))]
    private void AbrirEditarTipo(CatalogItemModel? item)
    {
        if (item is null) return;
        TipoEdicion = item.Clone();
        IsTipoDialogOpen = true;
    }

    [RelayCommand]
    private async Task GuardarTipoAsync()
    {
        if (TipoEdicion is null) return;
        try
        {
            var dto = await _service.ActualizarTipoAsync(TipoEdicion.Id, TipoEdicion.Nombre.Trim());
            var original = Tipos.First(t => t.Id == dto.Id);
            original.Nombre = dto.Nombre;
            IsTipoDialogOpen = false;
            TipoEdicion = null;
            MostrarNotificacion("Tipo actualizado", "Los cambios fueron guardados.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", ex.Message, true);
        }
    }

    [RelayCommand]
    private void CancelarEditarTipo()
    {
        TipoEdicion = null;
        IsTipoDialogOpen = false;
    }

    [RelayCommand(CanExecute = nameof(IsAdmin))]
    private async Task EliminarTipoAsync(CatalogItemModel? item)
    {
        if (item is null) return;
        if (MessageBox.Show($"Eliminar el tipo {item.Nombre}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            await _service.EliminarTipoAsync(item.Id);
            Tipos.Remove(item);
            MostrarNotificacion("Tipo eliminado", "El registro fue eliminado.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", ex.Message, true);
        }
    }

    [RelayCommand(CanExecute = nameof(IsAdmin))]
    private async Task AgregarProveedorAsync()
    {
        if (!NuevoProveedor.EsValido(out var mensaje))
        {
            MostrarNotificacion("Validacion", mensaje ?? "Complete los campos requeridos", true);
            return;
        }

        try
        {
            var dto = await _service.CrearProveedorAsync(NuevoProveedor.ToDto());
            Proveedores.Add(new ProveedorModel(dto));
            NuevoProveedor = new ProveedorFormModel();
            MostrarNotificacion("Proveedor creado", "El proveedor se registro correctamente.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", ex.Message, true);
        }
    }

    [RelayCommand(CanExecute = nameof(IsAdmin))]
    private void AbrirEditarProveedor(ProveedorModel? proveedor)
    {
        if (proveedor is null) return;
        ProveedorEdicion = ProveedorFormModel.FromModel(proveedor);
        IsProveedorDialogOpen = true;
    }

    [RelayCommand]
    private async Task GuardarProveedorAsync()
    {
        if (ProveedorEdicion is null || ProveedorEdicion.Id is null) return;
        if (!ProveedorEdicion.EsValido(out var mensaje))
        {
            MostrarNotificacion("Validacion", mensaje ?? "Complete los campos requeridos", true);
            return;
        }

        try
        {
            var dto = await _service.ActualizarProveedorAsync(ProveedorEdicion.Id.Value, ProveedorEdicion.ToDto());
            var original = Proveedores.First(p => p.Id == dto.Id);
            original.Actualizar(dto);
            IsProveedorDialogOpen = false;
            ProveedorEdicion = null;
            MostrarNotificacion("Proveedor actualizado", "Los cambios fueron guardados.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", ex.Message, true);
        }
    }

    [RelayCommand]
    private void CancelarEditarProveedor()
    {
        ProveedorEdicion = null;
        IsProveedorDialogOpen = false;
    }

    [RelayCommand(CanExecute = nameof(IsAdmin))]
    private async Task EliminarProveedorAsync(ProveedorModel? proveedor)
    {
        if (proveedor is null) return;
        if (MessageBox.Show($"Eliminar al proveedor {proveedor.RazonSocial}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            await _service.EliminarProveedorAsync(proveedor.Id);
            Proveedores.Remove(proveedor);
            MostrarNotificacion("Proveedor eliminado", "El registro fue eliminado.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", ex.Message, true);
        }
    }

    private async Task CargarAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Areas.Clear();
            var areas = await _service.ObtenerAreasAsync();
            foreach (var area in areas)
            {
                Areas.Add(new CatalogItemModel(area.Id, area.Nombre));
            }

            Tipos.Clear();
            var tipos = await _service.ObtenerTiposAsync();
            foreach (var tipo in tipos)
            {
                Tipos.Add(new CatalogItemModel(tipo.Id, tipo.Nombre));
            }

            Proveedores.Clear();
            var proveedores = await _service.ObtenerProveedoresAsync();
            foreach (var p in proveedores)
            {
                Proveedores.Add(new ProveedorModel(p));
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

    private void MostrarNotificacion(string titulo, string mensaje, bool esError = false)
    {
        NotifyTitle = titulo;
        NotifyMessage = mensaje;
        NotifyIsError = esError;
        IsNotifyOpen = true;
    }
}

