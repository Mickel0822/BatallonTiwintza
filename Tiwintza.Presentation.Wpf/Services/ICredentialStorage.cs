using System.Threading;
using System.Threading.Tasks;

namespace Tiwintza.Presentation.Wpf.Services;

public sealed record RememberMeCredential(string Username, string? Token = null);

public interface ICredentialStorage
{
    Task SaveAsync(RememberMeCredential cred, CancellationToken ct = default);
    Task<RememberMeCredential?> LoadAsync(CancellationToken ct = default);
    Task DeleteAsync(CancellationToken ct = default);
}
