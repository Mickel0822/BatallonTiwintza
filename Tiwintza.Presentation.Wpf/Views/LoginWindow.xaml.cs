using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Tiwintza.Presentation.Wpf.ViewModels;

namespace Tiwintza.Presentation.Wpf.Views
{
    public partial class LoginWindow : Window
    {
        private LoginViewModel VM => (LoginViewModel)DataContext;

        public LoginWindow(LoginViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            Loaded += async (_, __) => await vm.InitAsync();

            vm.LoginExitoso += () =>
            {
                var main = App.AppHost.Services.GetRequiredService<MainWindow>();
                main.Show();
                Close();
            };
        }

        // Sincroniza PasswordBox con el ViewModel (simple y suficiente)
        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            VM.Clave = PwdBox.Password;
        }
    }
}

