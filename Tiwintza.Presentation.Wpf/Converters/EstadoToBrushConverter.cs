using System.Globalization;
using System.Windows;          
using System.Windows.Data;
using System.Windows.Media;    

namespace Tiwintza.Presentation.Wpf.Converters;

public sealed class EstadoToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = (value as string)?.Trim().ToLowerInvariant();

        // Seguro en diseño / arranque: si no hay Application o el recurso no existe, usa Gray
        static Brush TryRes(string key)
            => Application.Current?.TryFindResource(key) as Brush ?? Brushes.Gray;

        return s switch
        {
            "baja" or "de baja" or "inactivo" => TryRes("MutedBrush"),
            "nuevo" or "bueno" or "operativo" => TryRes("SuccessBrush"),
            "regular" or "mantenimiento" => TryRes("WarningBrush"),
            "malo" or "dañado" => TryRes("ErrorBrush"),
            _ => TryRes("InfoBrush")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
