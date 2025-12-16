namespace Tiwintza.Infrastructure.Common;

public interface IAppUserAccessor
{
    string? CurrentUser { get; }
    bool IsAdmin { get; }
    void Set(string? username, bool isAdmin);
    void Clear();
}
