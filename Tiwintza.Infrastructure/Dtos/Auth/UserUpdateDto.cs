using System;
using System.Collections.Generic;

namespace Tiwintza.Infrastructure.Dtos.Auth;

public sealed class UserUpdateDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Password { get; set; }
    public Guid RoleId { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<Guid> SedeIds { get; set; } = Array.Empty<Guid>();
}
