using System.Windows;
using System.Windows.Controls;
using Tiwintza.Presentation.Wpf.ViewModels.Existencias;

namespace Tiwintza.Presentation.Wpf.Views.Existencias;

public partial class ConsultaStockView : UserControl
{
    public ConsultaStockView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConsultaStockViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
