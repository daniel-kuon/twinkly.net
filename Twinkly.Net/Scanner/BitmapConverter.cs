using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Scanner;

public class BitmapConverter : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        var path = (string)values[0];
        if (string.IsNullOrWhiteSpace(path)) return null;

        return new Bitmap(path);
    }

    public IList<object> ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}