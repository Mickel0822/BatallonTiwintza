using System;

namespace Tiwintza.Infrastructure.Dtos.Auth;

public sealed class UserCreateDto
{
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Password { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public bool IsActive { get; set; } = true;
}
