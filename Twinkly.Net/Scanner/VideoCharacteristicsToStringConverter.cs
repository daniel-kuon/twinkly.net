using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using FlashCap;

namespace Scanner;

public class VideoCharacteristicsToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is VideoCharacteristics videoCharacteristics)
        {
            return $"{videoCharacteristics.Width}x{videoCharacteristics.Height}";
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

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

public class InvertBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool booleanValue)
        {
            return !booleanValue;
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
