namespace Tiwintza.Infrastructure.Common;

public sealed class AppUserAccessor : IAppUserAccessor
{
    private string? _current;
    private bool _isAdmin;

    public string? CurrentUser => _current;
    public bool IsAdmin => _isAdmin;

    public void Set(string? username, bool isAdmin)
    {
        _current = string.IsNullOrWhiteSpace(username) ? null : username.Trim();
        _isAdmin = isAdmin;
    }

    public void Clear()
    {
        _current = null;
        _isAdmin = false;
    }
}
