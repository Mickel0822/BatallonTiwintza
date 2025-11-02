using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tiwintza.Infrastructure.Data;
using Tiwintza.Infrastructure.Data.Models;
using Tiwintza.Infrastructure.Dtos.Auth;

namespace Tiwintza.Infrastructure.Services.Auth;

public sealed class UsuariosService : IUsuariosService
{
    private readonly AppDbContext _db;

    public UsuariosService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserListItemDto>> ObtenerUsuariosAsync(CancellationToken ct = default)
    {
        var usuarios = await _db.Usuario
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
        var roles = await _db.Rol
            .AsNoTracking()
            .OrderBy(r => r.Nombre)
            .Select(r => new RoleOptionDto(r.Id, r.Nombre, r.Descripcion))
            .ToListAsync(ct);

        return roles;
    }

    public async Task<UserListItemDto> CrearUsuarioAsync(UserCreateDto dto, CancellationToken ct = default)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));

        var username = Normalize(dto.Username, nameof(dto.Username));
        var nombre = Normalize(dto.NombreCompleto, nameof(dto.NombreCompleto));
        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();

        if (await _db.Usuario.AnyAsync(u => EF.Functions.ILike(u.Username, username), ct))
            throw new InvalidOperationException("Ya existe un usuario con ese nombre de usuario.");

        if (!string.IsNullOrEmpty(email))
        {
            if (await _db.Usuario.AnyAsync(u => u.Email != null && EF.Functions.ILike(u.Email!, email), ct))
                throw new InvalidOperationException("Ya existe un usuario con ese correo.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Trim().Length < 6)
            throw new InvalidOperationException("La contraseña debe tener al menos 6 caracteres.");

        var role = await _db.Rol.FirstOrDefaultAsync(r => r.Id == dto.RoleId, ct)
                   ?? throw new KeyNotFoundException("Rol no encontrado.");

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

        _db.Usuario.Add(user);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(user).Collection(u => u.Rol).LoadAsync(ct);

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

    private static string Normalize(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El valor es requerido.", paramName);

        return value.Trim();
    }
}
