using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using Tiwintza.Presentation.Wpf.ViewModels;
using Tiwintza.Presentation.Wpf.Views;

namespace Tiwintza.Presentation.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            vm.LogoutRequested += OnLogoutRequested;
            Closed += OnClosed;
        }

        public void RefreshSession()
        {
            if (DataContext is MainViewModel vm)
            {
                vm.RefrescarSesion();
            }
        }

        private void OnLogoutRequested()
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            vm.RefrescarSesion();

            var login = App.AppHost.Services.GetRequiredService<LoginWindow>();
            login.Show();

            Hide();
        }

        private void UserMenuButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            if (button.ContextMenu is null)
            {
                return;
            }

            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.DataContext = button.DataContext;
            button.ContextMenu.IsOpen = true;
        }

        private void OnClosed(object? sender, System.EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.LogoutRequested -= OnLogoutRequested;
            }
        }
    }
}
