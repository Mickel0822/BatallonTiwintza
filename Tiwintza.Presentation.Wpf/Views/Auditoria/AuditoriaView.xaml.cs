using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Tiwintza.Presentation.Wpf.ViewModels.Auditoria;

namespace Tiwintza.Presentation.Wpf.Views.Auditoria;

public partial class AuditoriaView : UserControl
{
    private bool _initialized;

    public AuditoriaView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;

        if (DataContext is AuditoriaViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }

    private async void OnSorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;
        if (DataContext is not AuditoriaViewModel vm) return;

        var sortMember = e.Column.SortMemberPath;
        if (string.IsNullOrWhiteSpace(sortMember) && e.Column.Header is string header)
        {
            sortMember = header.ToLowerInvariant();
        }

        await vm.CambiarOrdenCommand.ExecuteAsync(sortMember);
    }
}
