using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Tiwintza.Infrastructure.Dtos.Catalogos;
using Tiwintza.Infrastructure.Services;

namespace Tiwintza.Presentation.Wpf.ViewModels.Catalogos;

public sealed partial class CatalogosViewModel : ObservableObject
{
    private readonly ICatalogosService _service;
    private bool _initialized;

    public CatalogosViewModel(ICatalogosService service)
    {
        _service = service;
        Areas = new ObservableCollection<CatalogItemModel>();
        Tipos = new ObservableCollection<CatalogItemModel>();
        Proveedores = new ObservableCollection<ProveedorModel>();
        NuevoProveedor = new ProveedorFormModel();
    }

    public ObservableCollection<CatalogItemModel> Areas { get; }
    public ObservableCollection<CatalogItemModel> Tipos { get; }
    public ObservableCollection<ProveedorModel> Proveedores { get; }

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

    [RelayCommand]
    private async Task AgregarAreaAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoAreaNombre))
        {
            MostrarNotificacion("Validacion", "Ingrese el nombre del area", true);
            return;
        }

        try
        {
            await _service.CrearAreaAsync(NuevoAreaNombre.Trim());
            NuevoAreaNombre = null;
            await CargarAsync();
            MostrarNotificacion("Area creada", "El area se registro correctamente.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", GetDetailedError(ex), true);
        }
    }

    [RelayCommand]
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
            await _service.ActualizarAreaAsync(AreaEdicion.Id, AreaEdicion.Nombre.Trim());
            IsAreaDialogOpen = false;
            AreaEdicion = null;
            await CargarAsync();
            MostrarNotificacion("Area actualizada", "Los cambios fueron guardados.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", GetDetailedError(ex), true);
        }
    }

    [RelayCommand]
    private void CancelarEditarArea()
    {
        AreaEdicion = null;
        IsAreaDialogOpen = false;
    }

    [RelayCommand]
    private async Task EliminarAreaAsync(CatalogItemModel? item)
    {
        if (item is null) return;
        if (MessageBox.Show($"Eliminar el area {item.Nombre}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            await _service.EliminarAreaAsync(item.Id);
            await CargarAsync();
            MostrarNotificacion("Area eliminada", "El registro fue eliminado.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", GetDetailedError(ex), true);
        }
    }

    [RelayCommand]
    private async Task AgregarTipoAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoTipoNombre))
        {
            MostrarNotificacion("Validacion", "Ingrese el nombre del tipo", true);
            return;
        }

        try
        {
            await _service.CrearTipoAsync(NuevoTipoNombre.Trim());
            NuevoTipoNombre = null;
            await CargarAsync();
            MostrarNotificacion("Tipo creado", "El tipo se registro correctamente.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", GetDetailedError(ex), true);
        }
    }

    [RelayCommand]
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
            await _service.ActualizarTipoAsync(TipoEdicion.Id, TipoEdicion.Nombre.Trim());
            IsTipoDialogOpen = false;
            TipoEdicion = null;
            await CargarAsync();
            MostrarNotificacion("Tipo actualizado", "Los cambios fueron guardados.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", GetDetailedError(ex), true);
        }
    }

    [RelayCommand]
    private void CancelarEditarTipo()
    {
        TipoEdicion = null;
        IsTipoDialogOpen = false;
    }

    [RelayCommand]
    private async Task EliminarTipoAsync(CatalogItemModel? item)
    {
        if (item is null) return;
        if (MessageBox.Show($"Eliminar el tipo {item.Nombre}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            await _service.EliminarTipoAsync(item.Id);
            await CargarAsync();
            MostrarNotificacion("Tipo eliminado", "El registro fue eliminado.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", GetDetailedError(ex), true);
        }
    }

    [RelayCommand]
    private async Task AgregarProveedorAsync()
    {
        if (!NuevoProveedor.EsValido(out var mensaje))
        {
            MostrarNotificacion("Validacion", mensaje ?? "Complete los campos requeridos", true);
            return;
        }

        try
        {
            await _service.CrearProveedorAsync(NuevoProveedor.ToDto());
            NuevoProveedor = new ProveedorFormModel();
            await CargarAsync();
            MostrarNotificacion("Proveedor creado", "El proveedor se registro correctamente.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", GetDetailedError(ex), true);
        }
    }

    [RelayCommand]
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
            await _service.ActualizarProveedorAsync(ProveedorEdicion.Id.Value, ProveedorEdicion.ToDto());
            IsProveedorDialogOpen = false;
            ProveedorEdicion = null;
            await CargarAsync();
            MostrarNotificacion("Proveedor actualizado", "Los cambios fueron guardados.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", GetDetailedError(ex), true);
        }
    }

    [RelayCommand]
    private void CancelarEditarProveedor()
    {
        ProveedorEdicion = null;
        IsProveedorDialogOpen = false;
    }

    [RelayCommand]
    private async Task EliminarProveedorAsync(ProveedorModel? proveedor)
    {
        if (proveedor is null) return;
        if (MessageBox.Show($"Eliminar al proveedor {proveedor.RazonSocial}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            await _service.EliminarProveedorAsync(proveedor.Id);
            await CargarAsync();
            MostrarNotificacion("Proveedor eliminado", "El registro fue eliminado.");
        }
        catch (Exception ex)
        {
            MostrarNotificacion("Error", GetDetailedError(ex), true);
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

    private string GetDetailedError(Exception ex)
    {
        var msg = ex.Message;
        var inner = ex.InnerException;
        while (inner != null)
        {
            msg += $"\n{inner.Message}";
            inner = inner.InnerException;
        }
        return msg;
    }

    private void MostrarNotificacion(string titulo, string mensaje, bool esError = false)
    {
        NotifyTitle = titulo;
        NotifyMessage = mensaje;
        NotifyIsError = esError;
        IsNotifyOpen = true;
    }
}

