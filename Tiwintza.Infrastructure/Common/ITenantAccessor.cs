using System;

namespace Tiwintza.Infrastructure.Common;

public sealed record SedeTenant(Guid Id, string Clave, string Nombre);

public interface ITenantAccessor
{
    SedeTenant? Current { get; }
    bool HasTenant { get; }
    void Set(SedeTenant tenant);
    void Clear();
}
