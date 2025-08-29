using System.Threading.Tasks;

namespace Tiwintza.Presentation.Wpf.Services
{
    public interface IAuthService
    {
        Task<bool> SignInAsync(string usuario, string clave);
    }
}
