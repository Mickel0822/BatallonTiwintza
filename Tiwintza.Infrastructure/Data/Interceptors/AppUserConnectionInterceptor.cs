using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tiwintza.Infrastructure.Common;

namespace Tiwintza.Infrastructure.Data.Interceptors;

public sealed class AppUserConnectionInterceptor : DbConnectionInterceptor
{
    private readonly IAppUserAccessor _userAccessor;

    public AppUserConnectionInterceptor(IAppUserAccessor userAccessor)
    {
        _userAccessor = userAccessor;
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await SetAppUserAsync(connection, cancellationToken).ConfigureAwait(false);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken).ConfigureAwait(false);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetAppUser(connection);
        base.ConnectionOpened(connection, eventData);
    }

    private async Task SetAppUserAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = BuildCommandText();
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private void SetAppUser(DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = BuildCommandText();
        cmd.ExecuteNonQuery();
    }

    private string BuildCommandText()
    {
        var username = _userAccessor.CurrentUser ?? "tiwintza-app";
        var sanitized = username.Replace("'", "''");
        return $"SET \"app.user\" = '{sanitized}';";
    }
}
