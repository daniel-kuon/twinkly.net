using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Scanner;

public class EnumValueItemConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            return Enum.Parse(targetType, stringValue);
        }

        throw new NotImplementedException();
    }
}