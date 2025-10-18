using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using OpenCvSharp;
using Point = Avalonia.Point;
using Rect = Avalonia.Rect;

namespace Scanner;

public class CustomImageControl : UserControl
{
    private static (double Top, double Left)? _mousePosition;
    private static (double Top, double Left)? _leftButtonFixedMarker;
    private static (double Top, double Left)? _rightButtonFixedMarker;
    private static (double Top, double Left)? _middleButtonFixedMarker;
    private (double Left, double Top)? _markerCenter;

    private static bool _isMouseOver;
    private readonly EllipseGeometry _highlightGeometry = new();

    private static event EventHandler? GlobalPointerEvent;

    private IImage? _source;

    public static readonly DirectProperty<CustomImageControl, IImage?> SourceProperty =
        AvaloniaProperty.RegisterDirect<CustomImageControl, IImage?>(
            nameof(Source),
            o => o.Source,
            (o, v) => o.Source = v);

    public IImage? Source
    {
        get => _source;
        set => SetAndRaise(SourceProperty, ref _source, value);
    }

    private string? _title;

    public static readonly DirectProperty<CustomImageControl, string?> TitleProperty =
        AvaloniaProperty.RegisterDirect<CustomImageControl, string?>(
            nameof(Title),
            o => o.Title,
            (o, v) => o.Title = v);

    public string? Title
    {
        get => _title;
        set => SetAndRaise(TitleProperty, ref _title, value);
    }


    public static readonly DirectProperty<CustomImageControl, (double X, double Y)?> MarkerCenterProperty =
        AvaloniaProperty.RegisterDirect<CustomImageControl, (double X, double Y)?>(
            nameof(MarkerCenter),
            o => o.MarkerCenter,
            (o, v) => o.MarkerCenter = v);

    private double _imageDrawingWidth;
    private double _imageDrawingHeight;
    private int _imageDrawingTop;
    private int _imageDrawingLeft;

    public (double X, double Y)? MarkerCenter
    {
        get => _markerCenter;
        set => SetAndRaise(MarkerCenterProperty, ref _markerCenter, value);
    }


    public CustomImageControl()
    {
        PointerMoved += OnLocalPointerMoved;
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        GlobalPointerEvent += OnGlobalPointerEvent;
        PointerPressed += OnPointerPressed;
        SourceProperty.Changed.Subscribe(_ => InvalidateVisual());
        TitleProperty.Changed.Subscribe(_ => InvalidateVisual());
        MarkerCenterProperty.Changed.Subscribe(_ => InvalidateVisual());
        InvalidateVisual();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pressedTyped = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
        if (pressedTyped == PointerUpdateKind.LeftButtonPressed)
        {
            if (e.ClickCount == 1)
            {
                _leftButtonFixedMarker = _mousePosition;
            }
            else if (e.ClickCount == 2)
            {
                _leftButtonFixedMarker = null;
            }
        }
        else if (pressedTyped == PointerUpdateKind.RightButtonPressed)
        {
            if (e.ClickCount == 1)
            {
                _rightButtonFixedMarker = _mousePosition;
            }
            else if (e.ClickCount == 2)
            {
                _rightButtonFixedMarker = null;
            }
        }
        else if (pressedTyped == PointerUpdateKind.MiddleButtonPressed)
        {
            if (e.ClickCount == 1)
            {
                _middleButtonFixedMarker = _mousePosition;
            }
            else if (e.ClickCount == 2)
            {
                _middleButtonFixedMarker = null;
            }
        }
    }

    private void OnLocalPointerMoved(object? sender, PointerEventArgs e)
    {
        var mousePosition = e.GetPosition(this);
        var relativeTop = (mousePosition.Y - _imageDrawingTop) / _imageDrawingHeight;
        var relativeLeft = (mousePosition.X - _imageDrawingLeft) / _imageDrawingWidth;
        _mousePosition = (relativeTop, relativeLeft);
        GlobalPointerEvent?.Invoke(this, EventArgs.Empty);
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        _isMouseOver = true;
        GlobalPointerEvent?.Invoke(this, EventArgs.Empty);
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        _isMouseOver = false;
        GlobalPointerEvent?.Invoke(this, EventArgs.Empty);
    }

    private void OnGlobalPointerEvent(object? sender, EventArgs eventArgs)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var title = Title;

        if (Source != null)
        {
            double scale = Math.Min(Bounds.Width / Source.Size.Width, Bounds.Height / Source.Size.Height);

            // Scaled image size
            _imageDrawingWidth = Source.Size.Width * scale;
            _imageDrawingHeight = Source.Size.Height * scale;
            _imageDrawingTop = (int)((Bounds.Height - _imageDrawingHeight) / 2);
            _imageDrawingLeft = (int)((Bounds.Width - _imageDrawingWidth) / 2);

            var destRect = new Rect(_imageDrawingLeft, _imageDrawingTop, _imageDrawingWidth, _imageDrawingHeight);
            context.DrawImage(Source, new Rect(Source.Size), destRect);
        }

        if (_isMouseOver && _mousePosition != null)
        {
            title += $" Mouse: {(int)_mousePosition.Value.Left}, {(int)_mousePosition.Value.Top}";
            DrawRing(Brushes.Red, _mousePosition.Value);
        }

        if (_leftButtonFixedMarker != null)
        {
            DrawRing(Brushes.Green, _leftButtonFixedMarker.Value);
        }

        if (_rightButtonFixedMarker != null)
        {
            DrawRing(Brushes.Blue, _rightButtonFixedMarker.Value);
        }

        if (_middleButtonFixedMarker != null)
        {
            DrawRing(Brushes.Yellow, _middleButtonFixedMarker.Value);
        }

        if (MarkerCenter != null)
        {
            DrawRing(Brushes.Purple, MarkerCenter.Value);
        }

        if (title != null)
        {
            // Render title
            var text = new FormattedText(
                title.Trim(),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                16.0, // size
                Brushes.Black
            );
            context.FillRectangle(Brushes.White, new Rect(0, 0, text.Width + 10, text.Height + 10));
            context.DrawText(text, new Point(5, 5));
        }

        void DrawRing(IImmutableSolidColorBrush brush, (double Top, double Left) mousePosition)
        {
            var centerPoint = new Point(mousePosition.Left * _imageDrawingWidth + _imageDrawingLeft,
                mousePosition.Top * _imageDrawingHeight + _imageDrawingTop);

            var outerEllipse = new EllipseGeometry(new Rect(centerPoint.X - 10, centerPoint.Y - 10, 20, 20));
            var innerEllipse = new EllipseGeometry(new Rect(centerPoint.X - 8, centerPoint.Y - 8, 16, 16));

            var combinedGeometry = new CombinedGeometry
            {
                Geometry1 = outerEllipse,
                Geometry2 = innerEllipse,
                GeometryCombineMode = GeometryCombineMode.Exclude
            };

            context.DrawGeometry(brush, null, combinedGeometry);
        }
    }
}
