using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows; // [DEBUG]
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Infrastructure.Data.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Tiwintza.Infrastructure.Services.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAppUserAccessor _userAccessor;
    private readonly ILogger<AuthService> _logger;

    public UserSession? Current { get; private set; }

    public AuthService(
        IDbContextFactory<AppDbContext> dbFactory,
        IAppUserAccessor userAccessor,
        ILogger<AuthService> logger)
    {
        _dbFactory = dbFactory;
        _userAccessor = userAccessor;
        _logger = logger;
    }

    public async Task<bool> LoginAsync(string username, string password, bool rememberMe = false, CancellationToken ct = default)
    {
        try
        {
            using var db = await _dbFactory.CreateDbContextAsync(ct);

            var user = await db.Usuario
                .Include(u => u.Rol)
                .Include(u => u.UsuarioSede)
                    .ThenInclude(us => us.Sede)
                .FirstOrDefaultAsync(u => u.Username == username || u.Email == username, ct);

            if (user == null)
            {
                _logger.LogWarning("Login fallido: usuario no encontrado {Username}", username);
                return false;
            }

            if (!user.IsActive)
            {
                 _logger.LogWarning("Login fallido: usuario inactivo {Username}", username);
                 return false;
            }
            
            // Check lockout
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                 _logger.LogWarning("Login fallido: usuario bloqueado {Username}", username);
                 return false;
            }

            // [DEBUG] Diagnóstico de contraseñas
            var debugHash = BCrypt.Net.BCrypt.HashPassword(password);
            
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                // [DEBUG] Mostrar ventana emergente directa
                MessageBox.Show($"[DEBUG: ALGORITMO BCRYPT]\n\nHash Generado (input): {debugHash}\n\nHash DB (guardado): {user.PasswordHash}", "DEBUG HASH", MessageBoxButton.OK, MessageBoxImage.Information);
                throw new Exception("Login debug - check popup");
            }

            // Login success
            user.LastLogin = DateTime.UtcNow;
            user.FailedAttempts = 0;
            user.LockoutEnd = null;
            await db.SaveChangesAsync(ct);

            var roles = user.Rol.Select(r => r.Nombre).ToArray();
            
            var sedes = new List<SedeTenant>();
            if (user.UsuarioSede != null)
            {
                foreach(var us in user.UsuarioSede)
                {
                    if (us.Sede != null)
                    {
                        sedes.Add(new SedeTenant(us.SedeId, us.Sede.Clave, us.Sede.Nombre));
                    }
                }
            }

            Current = new UserSession(user.Id, user.Username, user.NombreCompleto, roles, user.AreaId, sedes);
            
            var isAdmin = roles.Any(r => r.Equals("admin", StringComparison.OrdinalIgnoreCase) || r.Equals("administrador", StringComparison.OrdinalIgnoreCase));
            _userAccessor.Set(user.Username, isAdmin);

            _logger.LogInformation("Login exitoso {Username}", username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en LoginAsync");
            return false;
        }
    }

    public Task LogoutAsync(CancellationToken ct = default)
    {
        Current = null;
        _userAccessor.Clear();
        return Task.CompletedTask;
    }
}
