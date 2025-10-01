using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Tiwintza.Infrastructure.Dtos;
using Tiwintza.Presentation.Wpf.ViewModels.Activos;

namespace Tiwintza.Presentation.Wpf.Views.Activos
{
    public partial class ActivosView : UserControl
    {
        private bool _inited;

        public ActivosView()
        {
            InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_inited) return;
            _inited = true;

            if (DataContext is ActivosListViewModel vm)
                await vm.InitAsync();
        }

        private void ActivosGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not ActivosListViewModel vm) return;

            var row = FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row?.DataContext is not ActivoListItemDto dto) return;

            if (vm.MostrarAccionesCommand.CanExecute(dto))
            {
                vm.MostrarAccionesCommand.Execute(dto);
            }
        }

        private static T? FindParent<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current is not null)
            {
                if (current is T target) return target;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
