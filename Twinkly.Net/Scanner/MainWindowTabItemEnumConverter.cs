using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Scanner;

public class MainWindowTabItemEnumConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // convert from enum to tabitem index
        if (value is MainWindowTabItem enumValue)
        {
            return (int)enumValue;
        }
        return 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // convert index back to enum
        if(value is int index)
        {
            return (MainWindowTabItem)index;
        }

        throw new ArgumentException("Invalid value type");

    }
}