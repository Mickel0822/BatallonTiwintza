using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tiwintza.Presentation.Wpf.Converters;

public sealed class StringNullToVisibilityConverter : IValueConverter
{
    public bool CollapseWhenEmpty { get; set; } = true;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasContent = value switch
        {
            null => false,
            string s => !string.IsNullOrWhiteSpace(s),
            _ => true
        };

        if (hasContent)
        {
            return Visibility.Visible;
        }

        return CollapseWhenEmpty ? Visibility.Collapsed : Visibility.Hidden;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
