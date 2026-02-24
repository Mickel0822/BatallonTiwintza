using System;
using System.Globalization;
using System.Windows.Data;

namespace Tiwintza.Presentation.Wpf.Converters;

public class NullToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Retorna true si value es null (para habilitar botón cuando NO hay proveedor)
        return value == null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
