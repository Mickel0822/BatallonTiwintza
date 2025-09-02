// Helpers/UiSentinels.cs
using Tiwintza.Infrastructure.Dtos;
namespace Tiwintza.Presentation.Wpf.Helpers
{
    public static class UiSentinels
    {
        public static IdNombreDto Ninguno { get; } = new(0L, "— Ninguno —");
    }
}
