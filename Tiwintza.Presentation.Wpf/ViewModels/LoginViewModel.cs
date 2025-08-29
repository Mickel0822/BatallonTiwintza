using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Tiwintza.Presentation.Wpf.Services;

namespace Tiwintza.Presentation.Wpf.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _auth;

        [ObservableProperty] private string? usuario;
        [ObservableProperty] private string? clave;     // se setea desde PasswordBox
        [ObservableProperty] private bool recordarme;
        [ObservableProperty] private string? mensaje;

        public event Action? LoginExitoso;

        public LoginViewModel(IAuthService auth) => _auth = auth;

        [RelayCommand] 
        private async Task LoginAsync()
        {
            Mensaje = null;

            if (string.IsNullOrWhiteSpace(Usuario))
            {
                Mensaje = "Ingrese el usuario.";
                return;
            }
            if (string.IsNullOrWhiteSpace(Clave))
            {
                Mensaje = "Ingrese la contraseña.";
                return;
            }

            var ok = await _auth.SignInAsync(Usuario.Trim(), Clave);
            if (ok) LoginExitoso?.Invoke();
            else Mensaje = "Usuario o contraseña incorrectos.";
        }

        [RelayCommand] 
        private void Salir() => System.Windows.Application.Current.Shutdown();
    }
}
