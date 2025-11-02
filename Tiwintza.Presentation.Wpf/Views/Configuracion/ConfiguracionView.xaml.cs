using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Tiwintza.Presentation.Wpf.ViewModels.Configuracion;

namespace Tiwintza.Presentation.Wpf.Views.Configuracion;

public partial class ConfiguracionView : UserControl
{
    public ConfiguracionView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConfiguracionViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ConfiguracionViewModel oldVm)
        {
            oldVm.SolicitarLimpiarFormulario -= OnSolicitarLimpiarFormulario;
        }

        if (e.NewValue is ConfiguracionViewModel newVm)
        {
            newVm.SolicitarLimpiarFormulario += OnSolicitarLimpiarFormulario;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConfiguracionViewModel vm)
        {
            vm.SolicitarLimpiarFormulario -= OnSolicitarLimpiarFormulario;
        }
    }

    private void OnSolicitarLimpiarFormulario()
    {
        NewUserPasswordBox.Password = string.Empty;
        ConfirmPasswordBox.Password = string.Empty;
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConfiguracionViewModel vm && sender is PasswordBox pb)
        {
            vm.ActualizarPassword(pb.Password);
        }
    }

    private void ConfirmPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ConfiguracionViewModel vm && sender is PasswordBox pb)
        {
            vm.ActualizarConfirmacionPassword(pb.Password);
        }
    }
}
