using System.Globalization;
using System.Windows.Data;

namespace Tiwintza.Presentation.Wpf.Converters;

public sealed class DateOnlyToStringConverter : IValueConverter
{
    public string Format { get; set; } = "dd/MM/yyyy";

    // DateOnlyToStringConverter
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null) return string.Empty;
        if (value is DateOnly d) return d.ToString(Format, culture);
        if (value is DateTime dt) return DateOnly.FromDateTime(dt).ToString(Format, culture);
        return string.Empty;
    }


    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Si algún día editas celdas y quieres parsear de vuelta
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            if (DateOnly.TryParseExact(s, Format, culture, DateTimeStyles.None, out var d)) return d;
            if (DateOnly.TryParse(s, culture, DateTimeStyles.None, out d)) return d;
        }
        return Binding.DoNothing; // no rompas el binding en lectura
    }
}
