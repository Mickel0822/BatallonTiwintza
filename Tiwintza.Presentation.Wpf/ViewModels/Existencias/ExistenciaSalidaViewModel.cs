using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Dtos;
using Tiwintza.Infrastructure.Dtos.Existencias;
using Tiwintza.Infrastructure.Services;

namespace Tiwintza.Presentation.Wpf.ViewModels.Existencias;

public sealed partial class ExistenciaSalidaViewModel : ObservableValidator
{
    private readonly IExistenciasCrudService _crud;
    private readonly IExistenciasService _svc;

    public ExistenciaSalidaViewModel(IExistenciasCrudService crud, IExistenciasService svc)
    {
        _crud = crud;
        _svc = svc;

        Fecha = DateTime.Today;

        ErrorsChanged += (_, __) => OnPropertyChanged(nameof(PuedeGuardar));
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsBusy))
            {
                OnPropertyChanged(nameof(PuedeGuardar));
            }
            else if (e.PropertyName == nameof(ExistenciaId))
            {
                ValidateProperty(ExistenciaId, nameof(ExistenciaId));
            }
            else if (e.PropertyName == nameof(Fecha))
            {
                ValidateProperty(Fecha, nameof(Fecha));
            }
            else if (e.PropertyName == nameof(AreaId))
            {
                ValidateProperty(AreaId, nameof(AreaId));
            }
            else if (e.PropertyName == nameof(Cantidad))
            {
                ValidateProperty(Cantidad, nameof(Cantidad));
            }
        };
    }

    public ObservableCollection<ExistenciaComboItemDto> Productos { get; } = new();
    public ObservableCollection<IdNombreDto> Areas { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? errorMessage;

    [ObservableProperty] private ExistenciaComboItemDto? productoSeleccionado;

    [ObservableProperty, NotifyDataErrorInfo]
    [Required(ErrorMessage = "Seleccione un producto")]
    private long? existenciaId;

    [ObservableProperty, NotifyDataErrorInfo]
    [Range(1, int.MaxValue, ErrorMessage = "Ingrese una cantidad valida")]
    private int cantidad = 1;

    [ObservableProperty, NotifyDataErrorInfo]
    [Required(ErrorMessage = "Seleccione la fecha")]
    private DateTime? fecha;

    [ObservableProperty, NotifyDataErrorInfo]
    [Required(ErrorMessage = "Seleccione un area")]
    private long? areaId;

    [ObservableProperty] private string? responsable;
    [ObservableProperty] private string? observacion;
    [ObservableProperty] private int? stockDisponible;

    public bool PuedeGuardar => !IsBusy 
                                && !HasErrors 
                                && ExistenciaId is not null 
                                && AreaId is not null 
                                && Cantidad > 0 
                                && Fecha is not null;

    public event Action<long>? Guardado;
    public event Action? Cancelado;
    public event Action? VolverSolicitado;

    public async Task InicializarAsync()
    {
        await CargarProductosAsync();
        await CargarAreasAsync();
        Fecha ??= DateTime.Today;
    }

    [RelayCommand]
    private async Task CargarProductosAsync()
    {
        try
        {
            Productos.Clear();
            var data = await _crud.ObtenerProductosAsync();
            foreach (var item in data)
            {
                Productos.Add(item);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CargarAreasAsync()
    {
        try
        {
            Areas.Clear();
            var data = await _crud.ObtenerAreasAsync();
            foreach (var item in data)
            {
                Areas.Add(item);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
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

        if (StockDisponible is int stock && Cantidad > stock)
        {
            ErrorMessage = "La cantidad supera el stock disponible";
            return;
        }

        try
        {
            IsBusy = true;
            var dto = new ExistenciaSalidaCreateDto
            {
                ExistenciaId = ExistenciaId!.Value,
                Cantidad = Cantidad,
                AreaId = AreaId!.Value,
                Fecha = DateOnly.FromDateTime(Fecha ?? DateTime.Today),
                Responsable = string.IsNullOrWhiteSpace(Responsable) ? null : Responsable.Trim(),
                Observacion = string.IsNullOrWhiteSpace(Observacion) ? null : Observacion.Trim()
            };

            var salidaId = await _crud.RegistrarSalidaAsync(dto);
            Guardado?.Invoke(salidaId);
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

    partial void OnProductoSeleccionadoChanged(ExistenciaComboItemDto? value)
    {
        if (value is null)
        {
            ExistenciaId = null;
            StockDisponible = null;
            return;
        }

        ExistenciaId = value.Id;
        _ = CargarStockAsync(value.Id);
    }

    private async Task CargarStockAsync(long existenciaId)
    {
        try
        {
            var data = await _svc.ObtenerAsync(existenciaId);
            StockDisponible = data?.StockActual;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
