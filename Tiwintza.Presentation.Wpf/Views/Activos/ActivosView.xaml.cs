// ActivosView.xaml.cs
using System.Windows;
using System.Windows.Controls;

namespace Tiwintza.Presentation.Wpf.Views.Activos
{
    public partial class ActivosView : UserControl
    {
        public ActivosView() => InitializeComponent();

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.Activos.ActivosListViewModel vm)
                await vm.InitAsync(); // ← ya existe en el VM
        }
    }
}
