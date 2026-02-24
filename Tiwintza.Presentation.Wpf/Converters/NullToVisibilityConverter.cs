using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tiwintza.Presentation.Wpf.Converters;

/// <summary>
/// Convierte null a Collapsed y no-null a Visible.
/// Si se pasa ConverterParameter="Inverse", invierte la lógica.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNotNull = value != null;
        
        // Si hay parámetro, invertir la lógica
        if (parameter is string param && param.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
        {
            isNotNull = !isNotNull;
        }
        
        return isNotNull ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
