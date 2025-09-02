// Converters/ZeroToNullLongConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace Tiwintza.Presentation.Wpf.Converters
{
    public sealed class ZeroToNullLongConverter : IValueConverter
    {
        // VM -> UI: NO fuerces null a 0; deja pasar tal cual
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value; // null -> null (para que se muestre el placeholder)

        // UI -> VM: si el usuario elige "Ninguno" (Id = 0) => null
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return null;
            if (value is long l && l == 0) return null;
            if (value is string s && long.TryParse(s, out var ls)) return ls == 0 ? null : ls;
            return value;
        }
    }
}
