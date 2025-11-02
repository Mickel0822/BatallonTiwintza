using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using Tiwintza.Presentation.Wpf.ViewModels;

namespace Tiwintza.Presentation.Wpf.Views
{
    public partial class LoginWindow : Window
    {
        private bool _isSyncingPassword;
        private LoginViewModel VM => (LoginViewModel)DataContext;

        public LoginWindow(LoginViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            Loaded += async (_, __) => await vm.InitAsync();

            vm.LoginExitoso += () =>
            {
                var main = App.AppHost.Services.GetRequiredService<MainWindow>();
                main.RefreshSession();

                if (!main.IsVisible)
                {
                    main.Show();
                }
                else
                {
                    main.Activate();
                }

                Close();
            };
        }

        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingPassword)
            {
                return;
            }

            VM.Clave = PwdBox.Password;
            SyncPasswordToPlain();
        }

        private void PwdPlainBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingPassword)
            {
                return;
            }

            var text = PwdPlainBox.Text ?? string.Empty;
            VM.Clave = text;
            SyncPlainToPassword();
        }

        private void TogglePasswordVisibility_OnChecked(object sender, RoutedEventArgs e)
        {
            SyncPasswordToPlain();

            PwdBox.Visibility = Visibility.Collapsed;
            PwdPlainBox.Visibility = Visibility.Visible;

            TogglePasswordVisibility.ToolTip = "Ocultar contraseÃ±a";

            PwdPlainBox.Focus();
            PwdPlainBox.CaretIndex = PwdPlainBox.Text?.Length ?? 0;
        }

        private void TogglePasswordVisibility_OnUnchecked(object sender, RoutedEventArgs e)
        {
            SyncPlainToPassword();

            PwdPlainBox.Visibility = Visibility.Collapsed;
            PwdBox.Visibility = Visibility.Visible;

            TogglePasswordVisibility.ToolTip = "Mostrar contraseÃ±a";

            PwdBox.Focus();
        }

        private void SyncPasswordToPlain()
        {
            if (PwdPlainBox is null)
            {
                return;
            }

            var password = PwdBox.Password;
            if (PwdPlainBox.Text == password)
            {
                return;
            }

            _isSyncingPassword = true;
            try
            {
                PwdPlainBox.Text = password;
            }
            finally
            {
                _isSyncingPassword = false;
            }
        }

        private void SyncPlainToPassword()
        {
            if (PwdPlainBox is null)
            {
                return;
            }

            var text = PwdPlainBox.Text ?? string.Empty;
            if (PwdBox.Password == text)
            {
                return;
            }

            _isSyncingPassword = true;
            try
            {
                PwdBox.Password = text;
            }
            finally
            {
                _isSyncingPassword = false;
            }
        }
    }
}



