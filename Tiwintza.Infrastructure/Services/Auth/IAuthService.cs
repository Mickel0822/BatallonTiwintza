using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tiwintza.Infrastructure.Services.Auth
{
    public sealed record UserSession(Guid UserId, string UserName, string FullName, string[] Roles, long? AreaId);

    public interface IAuthService
    {
        UserSession? Current { get; }
        Task<bool> LoginAsync(string userOrEmail, string password, bool rememberMe = false, CancellationToken ct = default);
        Task LogoutAsync(CancellationToken ct = default);
    }

}
