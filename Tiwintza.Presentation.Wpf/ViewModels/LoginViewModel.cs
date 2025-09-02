using System;
using System.Threading.Tasks;
using System.Windows;                          // para Application.Current
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tiwintza.Infrastructure.Services.Auth;
using Tiwintza.Presentation.Wpf.Services;      // ICredentialStorage

namespace Tiwintza.Presentation.Wpf.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _auth;
        private readonly ICredentialStorage _creds;


        // Campos enlazables
        [ObservableProperty] private string? usuario;
        [ObservableProperty] private string? clave;          // se llena desde PasswordBox (attached behavior)
        [ObservableProperty] private bool recordarme;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? mensaje;

        public event Action? LoginExitoso;

        public LoginViewModel(IAuthService auth, ICredentialStorage creds)
        {
            _auth = auth;
            _creds = creds;
        }

        // Prefill de "Recordarme"
        public async Task InitAsync()
        {
            try
            {
                var saved = await _creds.LoadAsync();
                if (saved is not null && !string.IsNullOrWhiteSpace(saved.Username))
                {
                    Usuario = saved.Username;
                    Recordarme = true;
                }
            }
            catch { /* no bloquear la UI por recordarme */ }
        }

        // ---- Comando Ingresar ----
        [RelayCommand(CanExecute = nameof(PuedeIngresar))]
        private async Task LoginAsync()
        {
            Mensaje = null;
            IsBusy = true;
            LoginCommand.NotifyCanExecuteChanged();

            try
            {
                if (string.IsNullOrWhiteSpace(Usuario)) { Mensaje = "Ingrese el usuario."; return; }
                if (string.IsNullOrWhiteSpace(Clave)) { Mensaje = "Ingrese la contraseña."; return; }

                var ok = await _auth.LoginAsync(Usuario.Trim(), Clave, Recordarme);
                if (!ok) { Mensaje = "Usuario o contraseña incorrectos."; return; }

                // Persistencia opcional del “Recordarme” (nunca debe romper el login)
                try
                {
                    if (Recordarme && !string.IsNullOrWhiteSpace(Usuario))
                        await _creds.SaveAsync(new RememberMeCredential(Usuario.Trim()));
                    else
                        await _creds.DeleteAsync();
                }
                catch { /* best-effort */ }

                LoginExitoso?.Invoke();
            }
            catch (Exception ex)
            {
            #if DEBUG
                Mensaje = ex.GetBaseException().Message;   // detalle en Debug
            #else
                Mensaje = "Ocurrió un error al iniciar sesión.";
            #endif
            }
            finally
            {
                IsBusy = false;
                LoginCommand.NotifyCanExecuteChanged();
            }
        }

        private bool PuedeIngresar() =>
            !IsBusy && !string.IsNullOrWhiteSpace(Usuario) && !string.IsNullOrWhiteSpace(Clave);

        // ---- Comando Salir ----
        [RelayCommand]
        private void Salir() => Application.Current.Shutdown();

        // ---- Mantener CanExecute y limpiar mensajes al escribir ----
        partial void OnUsuarioChanged(string? value)
        {
            if (!string.IsNullOrEmpty(Mensaje)) Mensaje = null;
            LoginCommand.NotifyCanExecuteChanged();
        }

        partial void OnClaveChanged(string? value)
        {
            if (!string.IsNullOrEmpty(Mensaje)) Mensaje = null;
            LoginCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsBusyChanged(bool value) => LoginCommand.NotifyCanExecuteChanged();
    }
}
