using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace XPlayer.Desktop.Converters;

public class BoolToPasswordCharConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isVisible && isVisible)
        {
            return '\0'; // Return null char to disable password masking (show text)
        }
        return '•'; // Return bullet char to mask password
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
