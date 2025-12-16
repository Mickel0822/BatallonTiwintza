using System;

namespace Tiwintza.Infrastructure.Dtos.Auth;

public sealed record UserListItemDto(
    Guid Id,
    string Username,
    string NombreCompleto,
    string? Email,
    bool IsActive,
    DateTime CreadoEn,
    string[] Roles);
