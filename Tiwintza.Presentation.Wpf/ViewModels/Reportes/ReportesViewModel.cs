using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Dtos;
using Tiwintza.Infrastructure.Dtos.Existencias;
using Tiwintza.Infrastructure.Services;
using System.Linq;

namespace Tiwintza.Presentation.Wpf.ViewModels.Reportes;

public sealed partial class ReportesViewModel : ObservableObject
{
    private readonly IActivosService _activosService;
    private readonly IExistenciasService _existenciasService;
    private bool _initialized;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private ObservableCollection<ActivoListItemDto> activos = new();
    [ObservableProperty] private ObservableCollection<ExistenciaListItemDto> existencias = new();

    public ReportesViewModel(IActivosService activosService, IExistenciasService existenciasService)
    {
        _activosService = activosService;
        _existenciasService = existenciasService;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await CargarDatosAsync();
        _initialized = true;
    }

    [RelayCommand]
    private async Task CargarDatosAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            // Load Activos (Large page size for report)
            var activosFilter = new ActivoFiltro { Page = 1, PageSize = 1000 }; 
            var activosResult = await _activosService.BuscarAsync(activosFilter);
            Activos = new ObservableCollection<ActivoListItemDto>(activosResult.Items);

            // Load Existencias
            var estFilter = new ExistenciaFiltro { Page = 1, PageSize = 1000 };
            var estResult = await _existenciasService.BuscarAsync(estFilter);
            Existencias = new ObservableCollection<ExistenciaListItemDto>(estResult.Items);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
