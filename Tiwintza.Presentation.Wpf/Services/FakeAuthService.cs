using System.Threading.Tasks;

namespace Tiwintza.Presentation.Wpf.Services
{
    public class FakeAuthService : IAuthService
    {
        public Task<bool> SignInAsync(string usuario, string clave)
            => Task.FromResult(usuario == "admin" && clave == "123");
    }
}
