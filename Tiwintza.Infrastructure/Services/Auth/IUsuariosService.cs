using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Dtos.Auth;

namespace Tiwintza.Infrastructure.Services.Auth;

public interface IUsuariosService
{
    Task<IReadOnlyList<UserListItemDto>> ObtenerUsuariosAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RoleOptionDto>> ObtenerRolesAsync(CancellationToken ct = default);
    Task<UserListItemDto> CrearUsuarioAsync(UserCreateDto dto, CancellationToken ct = default);
}
