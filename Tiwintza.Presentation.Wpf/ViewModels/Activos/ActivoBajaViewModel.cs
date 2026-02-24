using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Dtos;
using Tiwintza.Infrastructure.Services;

namespace Tiwintza.Presentation.Wpf.ViewModels.Activos;

public sealed partial class ActivoBajaViewModel : ObservableObject
{
    private readonly IActivosCrudService _crud;

    public long ActivoId { get; private set; }
    public string Codigo { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;

    [ObservableProperty] private string codigoInformeTecnico = string.Empty;
    [ObservableProperty] private DateTime? fechaBaja;
    [ObservableProperty] private string responsable = string.Empty;
    [ObservableProperty] private string? observaciones;
    [ObservableProperty] private bool isBusy;

    public bool PuedeDarBaja =>
        !string.IsNullOrWhiteSpace(CodigoInformeTecnico)
        && !string.IsNullOrWhiteSpace(Responsable)
        && FechaBaja is not null;

    public event Action? VolverSolicitado;
    public event Action? DarBajaSolicitado;

    public ActivoBajaViewModel(IActivosCrudService crud)
    {
        _crud = crud;
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(PuedeDarBaja))
                OnPropertyChanged(nameof(PuedeDarBaja));
        };
    }

    public Task InicializarAsync()
    {
        FechaBaja ??= DateTime.Today;
        return Task.CompletedTask;
    }

    public void Initialize(ActivoListItemDto item)
    {
        ActivoId = item.Id;
        Codigo = item.Codigo;
        Nombre = item.Nombre;
    }

    [RelayCommand]
    private void Volver() => VolverSolicitado?.Invoke();

    [RelayCommand]
    private void Cancelar() => VolverSolicitado?.Invoke();

    [RelayCommand]
    private async Task DarBajaAsync()
    {
        if (!PuedeDarBaja) return;

        IsBusy = true;
        try
        {
            await _crud.DarBajaAsync(
                ActivoId,
                CodigoInformeTecnico,
                DateOnly.FromDateTime(FechaBaja!.Value.Date),
                Responsable,
                Observaciones);

            DarBajaSolicitado?.Invoke();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
