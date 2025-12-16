using System;
using System.Threading;

namespace Tiwintza.Infrastructure.Common;

public sealed class TenantAccessor : ITenantAccessor
{
    private SedeTenant? _current;

    public SedeTenant? Current => Volatile.Read(ref _current);

    public bool HasTenant => Current is not null;

    public void Set(SedeTenant tenant)
    {
        if (tenant is null) throw new ArgumentNullException(nameof(tenant));
        Volatile.Write(ref _current, tenant);
    }

    public void Clear() => Volatile.Write(ref _current, null);
}
