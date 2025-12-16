using System.Windows;
using System.Windows.Controls;
using Tiwintza.Presentation.Wpf.ViewModels;

namespace Tiwintza.Presentation.Wpf.Views;

public partial class DashboardView : UserControl
{
    private bool _initialized;

    public DashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;

        if (DataContext is DashboardViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
