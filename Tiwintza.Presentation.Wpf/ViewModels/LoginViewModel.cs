using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Services.Auth;
using Tiwintza.Infrastructure.Dtos.Auth;
using Tiwintza.Presentation.Wpf.Services;

namespace Tiwintza.Presentation.Wpf.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly ICredentialStorage _creds;
    private readonly ITenantAccessor _tenantAccessor;

    [ObservableProperty] private string? usuario;
    [ObservableProperty] private string? clave;
    [ObservableProperty] private bool recordarme;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? mensaje;
    [ObservableProperty] private IReadOnlyList<SedeTenant>? sedesDisponibles;
    [ObservableProperty] private SedeTenant? sedeSeleccionada;
    [ObservableProperty] private bool requiereSeleccionSede;

    public event Action? LoginExitoso;

    public LoginViewModel(IAuthService auth, ICredentialStorage creds, ITenantAccessor tenantAccessor)
    {
        _auth = auth;
        _creds = creds;
        _tenantAccessor = tenantAccessor;
    }


    public async Task InitAsync()
    {
        _tenantAccessor.Clear();
        try
        {
            var saved = await _creds.LoadAsync();
            if (saved is not null && !string.IsNullOrWhiteSpace(saved.Username))
            {
                Usuario = saved.Username;
                Recordarme = true;
            }
        }
        catch
        {
            // ignore
        }
    }

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

            try
            {
                if (Recordarme && !string.IsNullOrWhiteSpace(Usuario))
                    await _creds.SaveAsync(new RememberMeCredential(Usuario.Trim()));
                else
                    await _creds.DeleteAsync();
            }
            catch
            {
            }

            var session = _auth.Current ?? throw new InvalidOperationException("No se pudo obtener la sesión actual.");
            var sedes = session.Sedes ?? Array.Empty<SedeTenant>();
            if (sedes.Count == 0)
                throw new InvalidOperationException("El usuario no tiene una sede asignada.");

            var esAdmin = session.Roles.Any(IsAdminRole);
            if (esAdmin)
            {
                PrepararSeleccionSede(sedes);
                Mensaje = "Selecciona la sede con la que deseas trabajar.";
                return;
            }

            CompletarInicioSesion(sedes[0]);
        }
        catch (Exception ex)
        {
#if DEBUG
            Mensaje = ex.GetBaseException().Message;
#else
            Mensaje = "Ocurrió un error al iniciar sesión.";
#endif
        }
        finally
        {
            IsBusy = false;
            LoginCommand.NotifyCanExecuteChanged();
            ConfirmarSedeCommand.NotifyCanExecuteChanged();
        }
    }

    private bool PuedeIngresar() =>
        !IsBusy && !RequiereSeleccionSede &&
        !string.IsNullOrWhiteSpace(Usuario) &&
        !string.IsNullOrWhiteSpace(Clave);

    [RelayCommand]
    private void Salir() => Application.Current.Shutdown();

    [RelayCommand(CanExecute = nameof(PuedeConfirmarSede))]
    private void ConfirmarSede()
    {
        if (SedeSeleccionada is null) return;
        CompletarInicioSesion(SedeSeleccionada);
    }

    [RelayCommand]
    private async Task CancelarSeleccionSedeAsync()
    {
        await _auth.LogoutAsync();
        _tenantAccessor.Clear();
        LimpiarSeleccionSede();
        Mensaje = "Selección de sede cancelada. Ingrese nuevamente.";
    }

    private bool PuedeConfirmarSede() => RequiereSeleccionSede && !IsBusy && SedeSeleccionada is not null;

    private void PrepararSeleccionSede(IReadOnlyList<SedeTenant> sedes)
    {
        SedesDisponibles = sedes;
        SedeSeleccionada = sedes.FirstOrDefault();
        RequiereSeleccionSede = true;
    }

    private void CompletarInicioSesion(SedeTenant sede)
    {
        _tenantAccessor.Set(sede);
        LimpiarSeleccionSede();
        LoginExitoso?.Invoke();
    }

    private void LimpiarSeleccionSede()
    {
        RequiereSeleccionSede = false;
        SedesDisponibles = null;
        SedeSeleccionada = null;
    }

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

    partial void OnIsBusyChanged(bool value)
    {
        LoginCommand.NotifyCanExecuteChanged();
        ConfirmarSedeCommand.NotifyCanExecuteChanged();
    }

    partial void OnRequiereSeleccionSedeChanged(bool value)
    {
        LoginCommand.NotifyCanExecuteChanged();
        ConfirmarSedeCommand.NotifyCanExecuteChanged();
    }

    partial void OnSedeSeleccionadaChanged(SedeTenant? value) => ConfirmarSedeCommand.NotifyCanExecuteChanged();

    private static bool IsAdminRole(string role) =>
        role.Equals("administrador", StringComparison.OrdinalIgnoreCase) ||
        role.Equals("admin", StringComparison.OrdinalIgnoreCase);
}
