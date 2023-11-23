using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Scanner;

public partial class ExpandingControl : HeaderedContentControl
{
    public ExpandingControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<ExpandingControl, bool>(nameof(IsExpanded));

    protected readonly IMultiValueConverter ExpandedToHeightConverter = new ExpandedToHeightConverter();
}

public class ExpandedToHeightConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isExpanded = values[0] as bool? ?? false;
        var actualHeight = values[1] as double? ?? double.NaN;

        return isExpanded ? actualHeight : 0;
    }
}
