using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Dtos.Existencias;
using Tiwintza.Infrastructure.Services;

namespace Tiwintza.Presentation.Wpf.ViewModels.Existencias;

public sealed partial class ConsultaStockViewModel : ObservableObject
{
    private readonly IExistenciasService _service;
    private bool _initialized;

    [ObservableProperty] private string? textoBusqueda;
    [ObservableProperty] private bool soloCriticos;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private ObservableCollection<ExistenciaListItemDto> items = new();

    public ConsultaStockViewModel(IExistenciasService service)
    {
        _service = service;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await CargarAsync();
        _initialized = true;
    }

    [RelayCommand]
    private async Task BuscarAsync()
    {
        await CargarAsync();
    }

    [RelayCommand]
    private async Task LimpiarAsync()
    {
        TextoBusqueda = null;
        SoloCriticos = false;
        await CargarAsync();
    }

    partial void OnSoloCriticosChanged(bool value) => BuscarCommand.Execute(null);

    private async Task CargarAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var filtro = new ExistenciaFiltro
            {
                Texto = TextoBusqueda,
                Nivel = SoloCriticos ? ExistenciaNivelEstado.Critico : null,
                Page = 1,
                PageSize = 100 // Load decent amount for quick view
            };

            var result = await _service.BuscarAsync(filtro);
            Items = new ObservableCollection<ExistenciaListItemDto>(result.Items);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
