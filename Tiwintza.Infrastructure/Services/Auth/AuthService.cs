using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Infrastructure.Data.Models;

namespace Tiwintza.Infrastructure.Services.Auth
{
    public sealed class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AuthService>? _logger;

        private const int MaxFallos = 5;
        private static readonly TimeSpan Lockout = TimeSpan.FromMinutes(5);

        // BCrypt “dummy” para igualar tiempos cuando el usuario no existe/bloqueado/inactivo
        private const string DummyHash = "$2a$11$ABCDEFGHIJKLMNOPQRSTUV/2tP8yYQnR5m5jE0c7wF3xZ0YtQwJe";

        public UserSession? Current { get; private set; }

        public AuthService(AppDbContext db, ILogger<AuthService>? logger = null)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<bool> LoginAsync(string userOrEmail, string password, bool rememberMe = false, CancellationToken ct = default)
        {
            // username o email (case-insensitive con ILIKE de PostgreSQL)
            var user = await _db.Usuario
                .Include(u => u.Rol) // 👈 TU navegación real
                .SingleOrDefaultAsync(u =>
                    EF.Functions.ILike(u.Username, userOrEmail) ||
                    (u.Email != null && EF.Functions.ILike(u.Email!, userOrEmail)), ct);

            if (user is null)
            {
                _ = BCrypt.Net.BCrypt.Verify(password, DummyHash); // timing-safe
                await AuditAsync(null, false, "Usuario no existe", ct);
                return false;
            }

            // Nota: en tu modelo LockoutEnd es DateTime? (no Offset); usamos UtcNow para coherencia
            if (user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
            {
                _ = BCrypt.Net.BCrypt.Verify(password, DummyHash);
                await AuditAsync(user.Id, false, "Usuario bloqueado temporalmente", ct);
                return false;
            }

            if (!user.IsActive)
            {
                _ = BCrypt.Net.BCrypt.Verify(password, DummyHash);
                await AuditAsync(user.Id, false, "Usuario inactivo", ct);
                return false;
            }

            var ok = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!ok)
            {
                user.FailedAttempts++;
                if (user.FailedAttempts >= MaxFallos)
                {
                    user.LockoutEnd = DateTime.UtcNow.Add(Lockout);
                    user.FailedAttempts = 0;
                }
                await _db.SaveChangesAsync(ct);
                await AuditAsync(user.Id, false, "Password incorrecto", ct);
                return false;
            }

            // Éxito
            user.FailedAttempts = 0;
            user.LockoutEnd = null;
            user.LastLogin = DateTime.UtcNow;

            // (Opcional) rehash oportunista si subes el factor de trabajo
            // if (BCrypt.Net.BCrypt.PasswordNeedsRehash(user.PasswordHash, workFactor: 12)) {
            //     user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
            // }

            await _db.SaveChangesAsync(ct);

            // 👇 Tu colección real de roles
            var roles = (user.Rol ?? new System.Collections.Generic.List<Rol>())
                        .Select(r => r.Nombre)
                        .ToArray();

            // Reutiliza tu tipo UserSession
            Current = new UserSession(user.Id, user.Username, user.NombreCompleto, roles, user.AreaId);

            await AuditAsync(user.Id, true, null, ct);
            return true;
        }

        public Task LogoutAsync(CancellationToken ct = default)
        {
            Current = null;
            return Task.CompletedTask;
        }

        private async Task AuditAsync(Guid? userId, bool exito, string? detalle, CancellationToken ct)
        {
            try
            {
                _db.LoginAuditoria.Add(new LoginAuditoria
                {
                    UsuarioId = userId,
                    Exito = exito,
                    Detalle = detalle,
                    FechaHora = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Fallo auditoría de login");
            }
        }
    }
}
