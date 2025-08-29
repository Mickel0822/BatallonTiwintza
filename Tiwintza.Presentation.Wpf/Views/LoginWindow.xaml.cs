using System.Windows;
using Tiwintza.Presentation.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection; 

namespace Tiwintza.Presentation.Wpf.Views
{
    public partial class LoginWindow : Window
    {
        private LoginViewModel VM => (LoginViewModel)DataContext;

        public LoginWindow(LoginViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            // Cuando el login sea OK, abrimos MainWindow y cerramos Login
            vm.LoginExitoso += () =>
            {
                var main = App.AppHost.Services.GetRequiredService<MainWindow>();
                main.Show();
                this.Close();
            };
        }

        // Sincroniza PasswordBox con el ViewModel (simple y suficiente)
        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            VM.Clave = PwdBox.Password;
        }
    }
}
