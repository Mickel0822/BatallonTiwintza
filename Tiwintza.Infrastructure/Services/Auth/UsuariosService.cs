using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tiwintza.Infrastructure.Common;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Infrastructure.Data.Models;
using Tiwintza.Infrastructure.Dtos.Auth;

namespace Tiwintza.Infrastructure.Services.Auth;

public sealed class UsuariosService : IUsuariosService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public UsuariosService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<UserListItemDto>> ObtenerUsuariosAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var usuarios = await db.Usuario
            .AsNoTracking()
            .Include(u => u.Rol)
            .OrderBy(u => u.NombreCompleto)
            .Select(u => new UserListItemDto(
                u.Id,
                u.Username,
                u.NombreCompleto,
                u.Email,
                u.IsActive,
                u.CreadoEn,
                u.Rol
                    .OrderBy(r => r.Nombre)
                    .Select(r => r.Nombre)
                    .ToArray()))
            .ToListAsync(ct);

        return usuarios;
    }

    public async Task<IReadOnlyList<RoleOptionDto>> ObtenerRolesAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var roles = await db.Rol
            .AsNoTracking()
            .OrderBy(r => r.Nombre)
            .Select(r => new RoleOptionDto(r.Id, r.Nombre, r.Descripcion))
            .ToListAsync(ct);

        return roles;
    }

    public async Task<IReadOnlyList<SedeTenant>> ObtenerSedesAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Sede
            .AsNoTracking()
            .OrderBy(s => s.Nombre)
            .Select(s => new SedeTenant(s.Id, s.Clave, s.Nombre))
            .ToListAsync(ct);
    }

    public async Task<UserListItemDto> CrearUsuarioAsync(UserCreateDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (dto is null) throw new ArgumentNullException(nameof(dto));

        var username = Normalize(dto.Username, nameof(dto.Username));
        var nombre = Normalize(dto.NombreCompleto, nameof(dto.NombreCompleto));
        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();

        if (await db.Usuario.AnyAsync(u => EF.Functions.ILike(u.Username, username), ct))
            throw new InvalidOperationException("Ya existe un usuario con ese nombre de usuario.");

        if (!string.IsNullOrEmpty(email))
        {
            if (await db.Usuario.AnyAsync(u => u.Email != null && EF.Functions.ILike(u.Email!, email), ct))
                throw new InvalidOperationException("Ya existe un usuario con ese correo.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Trim().Length < 6)
            throw new InvalidOperationException("La contraseña debe tener al menos 6 caracteres.");

        var role = await db.Rol.FirstOrDefaultAsync(r => r.Id == dto.RoleId, ct)
                   ?? throw new KeyNotFoundException("Rol no encontrado.");

        var sedeIds = dto.SedeIds?.Where(id => id != Guid.Empty).Distinct().ToArray() ?? Array.Empty<Guid>();
        if (sedeIds.Length == 0)
            throw new InvalidOperationException("Debe seleccionar al menos una sede para el usuario.");

        var sedesValidas = await db.Sede
            .Where(s => sedeIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (sedesValidas.Count != sedeIds.Length)
            throw new InvalidOperationException("Alguna de las sedes seleccionadas no existe.");

        var user = new Usuario
        {
            Username = username,
            NombreCompleto = nombre,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password.Trim(), workFactor: 11),
            IsActive = dto.IsActive,
            CreadoEn = DateTime.UtcNow
        };

        user.Rol.Add(role);

        db.Usuario.Add(user);
        await db.SaveChangesAsync(ct);

        foreach (var sedeId in sedesValidas)
        {
            db.UsuarioSede.Add(new UsuarioSede
            {
                UsuarioId = user.Id,
                SedeId = sedeId
            });
        }

        await db.SaveChangesAsync(ct);
        await db.Entry(user).Collection(u => u.Rol).LoadAsync(ct);

        return new UserListItemDto(
            user.Id,
            user.Username,
            user.NombreCompleto,
            user.Email,
            user.IsActive,
            user.CreadoEn,
            user.Rol
                .OrderBy(r => r.Nombre)
                .Select(r => r.Nombre)
                .ToArray());
    }

    public async Task<UserListItemDto> ActualizarUsuarioAsync(UserUpdateDto dto, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (dto is null) throw new ArgumentNullException(nameof(dto));

        var user = await db.Usuario
            .Include(u => u.Rol)
            .Include(u => u.UsuarioSede)
            .FirstOrDefaultAsync(u => u.Id == dto.Id, ct);

        if (user is null)
            throw new KeyNotFoundException("El usuario no existe.");

        var nombre = Normalize(dto.NombreCompleto, nameof(dto.NombreCompleto));
        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();

        // If username is in DTO, should we update it? 
        // DTO has plain Username property now. 
        // User request: "permitir editarlo, cambiarle el nombre".
        // I will assume Name = NombreCompleto. 
        // But if I want to support Username edit:
        // var username = Normalize(dto.Username, nameof(dto.Username));
        // if (username != user.Username) check unicity...
        // I will implement it to allow Username change.
        
        var username = Normalize(dto.Username, nameof(dto.Username));

        if (!string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase))
        {
             if (await db.Usuario.AnyAsync(u => EF.Functions.ILike(u.Username, username), ct))
                throw new InvalidOperationException("Ya existe un usuario con ese nombre de usuario.");
             user.Username = username;
        }

        if (email != user.Email)
        {
             if (!string.IsNullOrEmpty(email))
             {
                 if (await db.Usuario.AnyAsync(u => u.Email != null && EF.Functions.ILike(u.Email!, email) && u.Id != dto.Id, ct))
                    throw new InvalidOperationException("Ya existe otro usuario con ese correo.");
             }
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            if (dto.Password.Trim().Length < 6)
                throw new InvalidOperationException("La contraseña debe tener al menos 6 caracteres.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password.Trim(), workFactor: 11);
        }

        var role = await db.Rol.FirstOrDefaultAsync(r => r.Id == dto.RoleId, ct)
                   ?? throw new KeyNotFoundException("Rol no encontrado.");
        
        var sedeIds = dto.SedeIds?.Where(id => id != Guid.Empty).Distinct().ToArray() ?? Array.Empty<Guid>();
        if (sedeIds.Length == 0)
            throw new InvalidOperationException("Debe seleccionar al menos una sede para el usuario.");

        var sedesValidas = await db.Sede
            .Where(s => sedeIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (sedesValidas.Count != sedeIds.Length)
            throw new InvalidOperationException("Alguna de las sedes seleccionadas no existe.");

        user.NombreCompleto = nombre;
        user.Email = email;
        user.IsActive = dto.IsActive;

        user.Rol.Clear();
        user.Rol.Add(role);

        db.UsuarioSede.RemoveRange(user.UsuarioSede);
        foreach (var sedeId in sedesValidas)
        {
            db.UsuarioSede.Add(new UsuarioSede
            {
                UsuarioId = user.Id,
                SedeId = sedeId
            });
        }

        await db.SaveChangesAsync(ct);
        await db.Entry(user).Collection(u => u.Rol).LoadAsync(ct);

        return new UserListItemDto(
            user.Id,
            user.Username,
            user.NombreCompleto,
            user.Email,
            user.IsActive,
            user.CreadoEn,
            user.Rol
                .OrderBy(r => r.Nombre)
                .Select(r => r.Nombre)
                .ToArray());
    }

    public async Task<UserUpdateDto?> ObtenerUsuarioParaEditarAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        
        var user = await db.Usuario
            .AsNoTracking()
            .Include(u => u.Rol)
            .Include(u => u.UsuarioSede)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user == null) return null;

        return new UserUpdateDto
        {
            Id = user.Id,
            Username = user.Username,
            NombreCompleto = user.NombreCompleto,
            Email = user.Email,
            IsActive = user.IsActive,
            RoleId = user.Rol.FirstOrDefault()?.Id ?? Guid.Empty,
            SedeIds = user.UsuarioSede.Select(us => us.SedeId).ToArray(),
            Password = null
        };
    }

    private static string Normalize(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El valor es requerido.", paramName);

        return value.Trim();
    }
}
