using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Dtos;
using Tiwintza.Infrastructure.Services;

namespace Tiwintza.Presentation.Wpf.ViewModels.Activos;

public sealed partial class ActivoTrasladoViewModel : ObservableObject
{
    private readonly IActivosCrudService _crud;

    public long ActivoId { get; private set; }
    public string Codigo { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;

    // Catálogos
    public ObservableCollection<IdNombreDto> Areas { get; } = new();

    [ObservableProperty] private long? areaDestinoId;
    [ObservableProperty] private DateTime? fecha;
    [ObservableProperty] private string? observacion;

    public bool PuedeTrasladar => AreaDestinoId is not null && Fecha is not null;

    public event Action? VolverSolicitado;
    public event Action? TrasladarSolicitado;

    public ActivoTrasladoViewModel(IActivosCrudService crud)
    {
        _crud = crud;
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(PuedeTrasladar))
                OnPropertyChanged(nameof(PuedeTrasladar));
        };
    }

    public async Task InicializarAsync()
    {
        Areas.Clear();
        var (areas, _, _, _) = await _crud.CatalogosFormAsync();
        foreach (var a in areas) Areas.Add(a);
        Fecha ??= DateTime.Today;
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
    private async Task TrasladarAsync()
    {
        if (!PuedeTrasladar) return;

        await _crud.TrasladarAsync(
            ActivoId,
            AreaDestinoId!.Value,
            DateOnly.FromDateTime(Fecha!.Value.Date),
            Observacion,
            null);

        TrasladarSolicitado?.Invoke();
    }
}
