using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tiwintza.Infrastructure.Common;

namespace Tiwintza.Infrastructure.Services.Auth;

public sealed record UserSession(Guid UserId, string UserName, string FullName, string[] Roles, long? AreaId, IReadOnlyList<SedeTenant> Sedes);

public interface IAuthService
{
    UserSession? Current { get; }
    Task<bool> LoginAsync(string userOrEmail, string password, bool rememberMe = false, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
}
