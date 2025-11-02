using System.Windows;
using System.Windows.Controls;
using Tiwintza.Presentation.Wpf.ViewModels.Catalogos;

namespace Tiwintza.Presentation.Wpf.Views.Catalogos;

public partial class CatalogosView : UserControl
{
    private bool _initialized;

    public CatalogosView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;

        if (DataContext is CatalogosViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
