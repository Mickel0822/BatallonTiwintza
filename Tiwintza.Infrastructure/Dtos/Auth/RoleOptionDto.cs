using System;

namespace Tiwintza.Infrastructure.Dtos.Auth;

public sealed record RoleOptionDto(Guid Id, string Nombre, string? Descripcion);
