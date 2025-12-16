using System.Windows;
using System.Windows.Controls;
using Tiwintza.Presentation.Wpf.ViewModels.Existencias;

namespace Tiwintza.Presentation.Wpf.Views.Existencias;

public partial class ExistenciasView : UserControl
{
    private bool _loaded;

    public ExistenciasView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_loaded) return;
        _loaded = true;

        if (DataContext is ExistenciasListViewModel vm)
        {
            await vm.InitAsync();
        }
    }
}
