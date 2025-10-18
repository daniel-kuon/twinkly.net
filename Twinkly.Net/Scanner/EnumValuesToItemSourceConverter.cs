using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace Scanner;

public class EnumValuesToItemSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Type { IsEnum: true } type)
        {
            return Enum.GetValues(type);
        }

        if (value != null && value.GetType().IsEnum)
        {
            return Enum.GetValues(value.GetType());
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
