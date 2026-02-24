using System.Windows;
using System.Windows.Controls;
using Tiwintza.Presentation.Wpf.ViewModels.Reportes;

namespace Tiwintza.Presentation.Wpf.Views.Reportes;

public partial class ReportesView : UserControl
{
    public ReportesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ReportesViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }

    private void BtnImprimirActivos_Click(object sender, RoutedEventArgs e)
    {
        ImprimirGrilla(GridActivos, "Reporte Toma Física - Activos");
    }

    private void BtnImprimirExistencias_Click(object sender, RoutedEventArgs e)
    {
        ImprimirGrilla(GridExistencias, "Reporte Toma Física - Existencias");
    }

    private void ImprimirGrilla(DataGrid grid, string title)
    {
        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
        {
            // Opcional: Ajustar escala o layout temporalmente
            printDialog.PrintVisual(grid, title);
        }
    }
}
