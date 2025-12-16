
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    private bool _suspendSelectAllPropagation;
    
    private bool _suspendItemSelectionSync;
    private Guid? _usuarioEnEdicionId;


    public ConfiguracionViewModel(IUsuariosService usuariosService, IAuthService authService)
    {
        _usuariosService = usuariosService;
        _authService = authService;

        var roles = authService.Current?.Roles ?? Array.Empty<string>();
        TieneAccesoAdministrador = roles.Any(IsAdminRole);
    }

    public ObservableCollection<UsuarioItemViewModel> Usuarios { get; } = new();
    public ObservableCollection<RoleOptionViewModel> Roles { get; } = new();

    public ObservableCollection<SedeOptionViewModel> Sedes { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isDialogBusy;
    [ObservableProperty] private bool isNuevoUsuarioAbierto;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TituloDialogo))] private bool isEditing;
    [ObservableProperty] private string? mensajeDialogo;
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string? nuevoNombreCompleto;
    [ObservableProperty] private string? nuevoUsername;
    [ObservableProperty] private string? nuevoEmail;
    [ObservableProperty] private RoleOptionViewModel? rolSeleccionado;

    [ObservableProperty] private bool todasLasSedesSeleccionadas;
    [ObservableProperty] private bool nuevoUsuarioActivo = true;
    
    public string TituloDialogo => IsEditing ? "Editar usuario" : "Registrar usuario";

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
        await CargarSedesAsync();
        await CargarUsuariosAsync();
    }

    [RelayCommand]
    private async Task RecargarAsync()
    {
        if (!TieneAccesoAdministrador) return;
        await CargarUsuariosAsync();
        await CargarSedesAsync();
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
    private async Task EditarUsuarioAsync(UsuarioItemViewModel item)
    {
        if (!TieneAccesoAdministrador) return;
        
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            
            var dto = await _usuariosService.ObtenerUsuarioParaEditarAsync(item.Id);
            if (dto == null)
            {
                ErrorMessage = "El usuario no existe o fue eliminado.";
                await CargarUsuariosAsync();
                return;
            }

            LimpiarFormulario(); // Reset state
            
            _usuarioEnEdicionId = dto.Id;
            IsEditing = true;
            
            NuevoNombreCompleto = dto.NombreCompleto;
            NuevoUsername = dto.Username;
            NuevoEmail = dto.Email;
            NuevoUsuarioActivo = dto.IsActive;
            
            RolSeleccionado = Roles.FirstOrDefault(r => r.Id == dto.RoleId);
            
            _suspendItemSelectionSync = true;
            foreach (var sede in Sedes)
            {
                sede.IsSelected = dto.SedeIds.Contains(sede.Id);
            }
            _suspendItemSelectionSync = false;
            SincronizarSeleccionGeneral();
            
            IsNuevoUsuarioAbierto = true;
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

        var sedesSeleccionadas = Sedes.Where(s => s.IsSelected).Select(s => s.Id).ToArray();
        if (sedesSeleccionadas.Length == 0)
        {
            MensajeDialogo = "Seleccione al menos una sede.";
            return;
        }

        if (!IsEditing && (string.IsNullOrWhiteSpace(_nuevoPassword) || _nuevoPassword!.Length < 6))
        {
            MensajeDialogo = "La contraseña debe tener al menos 6 caracteres.";
            return;
        }

        if ((!string.IsNullOrWhiteSpace(_nuevoPassword) || !string.IsNullOrWhiteSpace(_confirmPassword)) && 
            !string.Equals(_nuevoPassword, _confirmPassword, StringComparison.Ordinal))
        {
            MensajeDialogo = "La confirmación de la contraseña no coincide.";
            return;
        }

        try
        {
            IsDialogBusy = true;
            
            if (IsEditing)
            {
                 var updateDto = new UserUpdateDto
                 {
                     Id = _usuarioEnEdicionId!.Value,
                     NombreCompleto = NuevoNombreCompleto.Trim(),
                     Username = NuevoUsername.Trim(),
                     Email = string.IsNullOrWhiteSpace(NuevoEmail) ? null : NuevoEmail!.Trim(),
                     Password = _nuevoPassword,
                     RoleId = RolSeleccionado.Id,
                     IsActive = NuevoUsuarioActivo,
                     SedeIds = sedesSeleccionadas
                 };
                 
                 var updated = await _usuariosService.ActualizarUsuarioAsync(updateDto);
                 
                 var oldItem = Usuarios.FirstOrDefault(u => u.Id == updated.Id);
                 if (oldItem != null) Usuarios.Remove(oldItem);
                 InsertarOrdenado(UsuarioItemViewModel.FromDto(updated));
            }
            else
            {
                var createDto = new UserCreateDto
                {
                    NombreCompleto = NuevoNombreCompleto.Trim(),
                    Username = NuevoUsername.Trim(),
                    Email = string.IsNullOrWhiteSpace(NuevoEmail) ? null : NuevoEmail!.Trim(),
                    Password = _nuevoPassword!,
                    RoleId = RolSeleccionado.Id,
                    IsActive = NuevoUsuarioActivo,
                    SedeIds = sedesSeleccionadas
                };
                
                var creado = await _usuariosService.CrearUsuarioAsync(createDto);
                InsertarOrdenado(UsuarioItemViewModel.FromDto(creado));
            }

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

    private async Task CargarSedesAsync()
    {
        try
        {
            foreach (var sede in Sedes)
            {
                sede.PropertyChanged -= OnSedeOptionPropertyChanged;
            }

            Sedes.Clear();

            var sedes = await _usuariosService.ObtenerSedesAsync();
            foreach (var sede in sedes)
            {
                var sedeVm = new SedeOptionViewModel(sede.Id, sede.Nombre);
                sedeVm.PropertyChanged += OnSedeOptionPropertyChanged;
                Sedes.Add(sedeVm);
            }

            _suspendSelectAllPropagation = false;
            _suspendItemSelectionSync = false;
            SincronizarSeleccionGeneral();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void OnSedeOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SedeOptionViewModel.IsSelected))
        {
            SincronizarSeleccionGeneral();
        }
    }

    private void SincronizarSeleccionGeneral()
    {
        if (_suspendItemSelectionSync) return;

        _suspendSelectAllPropagation = true;
        TodasLasSedesSeleccionadas = Sedes.Count > 0 && Sedes.All(s => s.IsSelected);
        _suspendSelectAllPropagation = false;
    }

    partial void OnTodasLasSedesSeleccionadasChanged(bool value)
    {
        if (_suspendSelectAllPropagation) return;

        _suspendItemSelectionSync = true;
        foreach (var sede in Sedes)
        {
            sede.IsSelected = value;
        }
        _suspendItemSelectionSync = false;
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
        TodasLasSedesSeleccionadas = false;
        MensajeDialogo = null;
        _usuarioEnEdicionId = null;
        IsEditing = false;
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

    public sealed partial class SedeOptionViewModel : ObservableObject
    {
        public SedeOptionViewModel(Guid id, string nombre)
        {
            Id = id;
            Nombre = nombre;
        }

        public Guid Id { get; }
        public string Nombre { get; }

        [ObservableProperty] private bool isSelected;

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








