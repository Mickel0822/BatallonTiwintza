
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Dtos.Auth;
using Tiwintza.Infrastructure.Services.Auth;

namespace Tiwintza.Presentation.Wpf.ViewModels.Configuracion;

public sealed partial class ConfiguracionViewModel : ObservableObject
{
    private readonly IUsuariosService _usuariosService;
    private readonly IAuthService _authService;
    private bool _initialized;
    private string? _nuevoPassword;
    private string? _confirmPassword;

    public ConfiguracionViewModel(IUsuariosService usuariosService, IAuthService authService)
    {
        _usuariosService = usuariosService;
        _authService = authService;

        var roles = authService.Current?.Roles ?? Array.Empty<string>();
        TieneAccesoAdministrador = roles.Any(IsAdminRole);
    }

    public ObservableCollection<UsuarioItemViewModel> Usuarios { get; } = new();
    public ObservableCollection<RoleOptionViewModel> Roles { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isDialogBusy;
    [ObservableProperty] private bool isNuevoUsuarioAbierto;
    [ObservableProperty] private string? mensajeDialogo;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string? nuevoNombreCompleto;
    [ObservableProperty] private string? nuevoUsername;
    [ObservableProperty] private string? nuevoEmail;
    [ObservableProperty] private RoleOptionViewModel? rolSeleccionado;
    [ObservableProperty] private bool nuevoUsuarioActivo = true;

    public bool TieneAccesoAdministrador { get; }

    public event Action? SolicitarLimpiarFormulario;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        if (!TieneAccesoAdministrador)
        {
            ErrorMessage = "La secci?n de configuraci?n solo est? disponible para administradores.";
            return;
        }

        await CargarRolesAsync();
        await CargarUsuariosAsync();
    }

    [RelayCommand]
    private async Task RecargarAsync()
    {
        if (!TieneAccesoAdministrador) return;
        await CargarUsuariosAsync();
    }

    [RelayCommand]
    private void AbrirNuevoUsuario()
    {
        if (!TieneAccesoAdministrador) return;
        LimpiarFormulario();
        IsNuevoUsuarioAbierto = true;
        SolicitarLimpiarFormulario?.Invoke();
    }

    [RelayCommand]
    private void CerrarNuevoUsuario()
    {
        IsNuevoUsuarioAbierto = false;
        MensajeDialogo = null;
        SolicitarLimpiarFormulario?.Invoke();
    }

    [RelayCommand]
    private async Task CrearNuevoUsuarioAsync()
    {
        if (!TieneAccesoAdministrador) return;

        MensajeDialogo = null;

        if (string.IsNullOrWhiteSpace(NuevoNombreCompleto))
        {
            MensajeDialogo = "Ingrese el nombre completo.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NuevoUsername))
        {
            MensajeDialogo = "Ingrese el nombre de usuario.";
            return;
        }

        if (RolSeleccionado is null)
        {
            MensajeDialogo = "Seleccione el rol para el usuario.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_nuevoPassword) || _nuevoPassword!.Length < 6)
        {
            MensajeDialogo = "La contrase?a debe tener al menos 6 caracteres.";
            return;
        }

        if (!string.Equals(_nuevoPassword, _confirmPassword, StringComparison.Ordinal))
        {
            MensajeDialogo = "La confirmaci?n de la contrase?a no coincide.";
            return;
        }

        var dto = new UserCreateDto
        {
            NombreCompleto = NuevoNombreCompleto.Trim(),
            Username = NuevoUsername.Trim(),
            Email = string.IsNullOrWhiteSpace(NuevoEmail) ? null : NuevoEmail!.Trim(),
            Password = _nuevoPassword!,
            RoleId = RolSeleccionado.Id,
            IsActive = NuevoUsuarioActivo
        };

        try
        {
            IsDialogBusy = true;
            var creado = await _usuariosService.CrearUsuarioAsync(dto);
            InsertarOrdenado(UsuarioItemViewModel.FromDto(creado));
            IsNuevoUsuarioAbierto = false;
            LimpiarFormulario();
            SolicitarLimpiarFormulario?.Invoke();
        }
        catch (Exception ex)
        {
            MensajeDialogo = ex.Message;
        }
        finally
        {
            IsDialogBusy = false;
        }
    }

    public void ActualizarPassword(string value) => _nuevoPassword = value;
    public void ActualizarConfirmacionPassword(string value) => _confirmPassword = value;

    private async Task CargarUsuariosAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            Usuarios.Clear();

            var usuarios = await _usuariosService.ObtenerUsuariosAsync();
            foreach (var usuario in usuarios.Select(UsuarioItemViewModel.FromDto))
            {
                InsertarOrdenado(usuario);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CargarRolesAsync()
    {
        try
        {
            Roles.Clear();
            var roles = await _usuariosService.ObtenerRolesAsync();
            foreach (var role in roles)
            {
                Roles.Add(new RoleOptionViewModel(role.Id, role.Nombre));
            }

            RolSeleccionado = Roles.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void LimpiarFormulario()
    {
        NuevoNombreCompleto = null;
        NuevoUsername = null;
        NuevoEmail = null;
        NuevoUsuarioActivo = true;
        RolSeleccionado = Roles.FirstOrDefault();
        _nuevoPassword = null;
        _confirmPassword = null;
        MensajeDialogo = null;
    }

    private void InsertarOrdenado(UsuarioItemViewModel item)
    {
        var index = 0;
        while (index < Usuarios.Count && string.Compare(Usuarios[index].NombreCompleto, item.NombreCompleto, StringComparison.CurrentCultureIgnoreCase) < 0)
        {
            index++;
        }

        Usuarios.Insert(index, item);
    }

    private static bool IsAdminRole(string role) =>
        role.Equals("administrador", StringComparison.OrdinalIgnoreCase) ||
        role.Equals("admin", StringComparison.OrdinalIgnoreCase);

    public sealed record RoleOptionViewModel(Guid Id, string Nombre)
    {
        public override string ToString() => Nombre;
    }

    public sealed record UsuarioItemViewModel(Guid Id,
                                              string Username,
                                              string NombreCompleto,
                                              string? Email,
                                              bool IsActive,
                                              string Roles,
                                              string CreadoEn)
    {
        public static UsuarioItemViewModel FromDto(UserListItemDto dto)
        {
            var roles = dto.Roles is { Length: > 0 }
                ? string.Join(", ", dto.Roles)
                : "Sin rol";

            var creado = dto.CreadoEn.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);

            return new UsuarioItemViewModel(dto.Id,
                                            dto.Username,
                                            dto.NombreCompleto,
                                            dto.Email,
                                            dto.IsActive,
                                            roles,
                                            creado);
        }
    }
}
