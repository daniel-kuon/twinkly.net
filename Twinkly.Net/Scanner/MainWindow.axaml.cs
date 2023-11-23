using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DynamicData;
using DynamicData.Binding;
using FlashCap;
using Bitmap = System.Drawing.Bitmap;
using Image = System.Drawing.Image;

namespace Scanner;

[SuppressMessage("Interoperability", "CA1416:Plattformkompatibilität überprüfen")]
public partial class MainWindow : Window
{
    private const int SampleSize = 10;
    private readonly ChannelProcessor _blueChannelProcessor;
    private readonly TimingCapture _blueTimingCapture;
    private readonly ChannelProcessor _greenChannelProcessor;
    private readonly TimingCapture _greenTimingCapture;

    private readonly ChannelProcessor _redChannelProcessor;
    private readonly TimingCapture _redTimingCapture;

    private readonly TimingCapture _timingCapture = new(SampleSize);
    private readonly MainWindowViewModel _viewModel;
    private ObservableCaptureDevice? _activeCamera;
    private readonly LedController _ledController;

    protected void PrepareClosing()
    {
        Settings.FromViewModel(_viewModel).Save();
        _ledController.Dispose();
    }

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        _redTimingCapture = new TimingCapture(SampleSize, _timingCapture);
        _greenTimingCapture = new TimingCapture(SampleSize, _timingCapture);
        _blueTimingCapture = new TimingCapture(SampleSize, _timingCapture);
        DataContext = _viewModel;

        _redTimingCapture.OnTimingStringUpdated += (sender, timings) => _viewModel.RedTimings = timings;
        _greenTimingCapture.OnTimingStringUpdated += (sender, timings) => _viewModel.GreenTimings = timings;
        _blueTimingCapture.OnTimingStringUpdated += (sender, timings) => _viewModel.BlueTimings = timings;

        _redChannelProcessor = new ChannelProcessor(_viewModel, _redTimingCapture);
        _greenChannelProcessor = new ChannelProcessor(_viewModel, _greenTimingCapture);
        _blueChannelProcessor = new ChannelProcessor(_viewModel, _blueTimingCapture);

        _viewModel.CameraList.AddRange(new CaptureDevices().EnumerateDescriptors());

        _viewModel.WhenValueChanged(v => v.SelectedCamera, false).Subscribe(async camera =>
        {
            if (_activeCamera is { } activeCamera)
            {
                await activeCamera.StopAsync();
                activeCamera.Dispose();
            }

            if (camera is null) return;

            _viewModel.CharacteristicList.Clear();
            _viewModel.CharacteristicList.AddRange(camera.Characteristics.Where(c => c.PixelFormat == PixelFormats.JPEG)
                .GroupBy(c => (c.Width, c.Height))
                .Select(g => g.OrderByDescending(c => c.FramesPerSecond).First()));

            _viewModel.SelectedCharacteristic = _viewModel.CharacteristicList.FirstOrDefault();
        });

        _viewModel.WhenValueChanged(v => v.SelectedCharacteristic, false).Subscribe(async characteristic =>
        {
            if (_activeCamera is { } activeCamera)
            {
                activeCamera.Dispose();
            }

            if (_viewModel.SelectedCamera is null || characteristic is null) return;

            _activeCamera = await _viewModel.SelectedCamera.AsObservableAsync(characteristic);
            _activeCamera.Subscribe(ActiveCameraOnNewFrame);
            await _activeCamera.StartAsync();
        });

        _viewModel.WhenValueChanged(v => v.ChannelSeparationMode, false).Subscribe(mode =>
        {
            _redChannelProcessor.ClearFrameCache();
            _greenChannelProcessor.ClearFrameCache();
            _blueChannelProcessor.ClearFrameCache();
        });

        Settings settings = Settings.Load();
        settings.SetToViewModel(_viewModel);

        _ledController = new LedController(_viewModel, _redChannelProcessor, _greenChannelProcessor, _blueChannelProcessor);

        this.Closing += (sender, args) => PrepareClosing();
    }

    private async void ActiveCameraOnNewFrame(PixelBufferScope bufferScope)
    {
        Bitmap bitmap;
        using (_timingCapture.CreateWatch("Create Bitmap"))
        {
            bitmap = new Bitmap(new MemoryStream(bufferScope.Buffer.ExtractImage()));
        }


        if (_viewModel.ScaleDownFactor > 1) bitmap = DownscaleBitmap(bitmap, _viewModel.ScaleDownFactor);

        if (_viewModel.TrimImage) bitmap = TrimBitmap(bitmap);

        var (redChannelArr, greenChannelArr, blueChannelArr) = ConvertToColorChannelArrays(bitmap);

        var tasks = new[]
        {
            Task.Run(() => _viewModel.RedChannel = _redChannelProcessor.ProcessChanel(redChannelArr)),
            Task.Run(() => _viewModel.GreenChannel = _greenChannelProcessor.ProcessChanel(greenChannelArr)),
            Task.Run(() => _viewModel.BlueChannel = _blueChannelProcessor.ProcessChanel(blueChannelArr))
        };

        await Task.WhenAll(tasks);

        _viewModel.LastRawFrame = ImageByteArrayConverter.ConvertToImage(_redChannelProcessor.LastProcessedFrame,
            _greenChannelProcessor.LastProcessedFrame, _blueChannelProcessor.LastProcessedFrame);
    }

    private (byte[,] redChannelArr, byte[,] greenChannelArr, byte[,] blueChannelArr) ConvertToColorChannelArrays(
        Bitmap bitmap)
    {
        using (_timingCapture.CreateWatch("Convert To Channel Arrays"))
        {
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                bitmap.PixelFormat);
            var bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            var redChannelArr = new byte[bitmap.Height, bitmap.Width];
            var greenChannelArr = new byte[bitmap.Height, bitmap.Width];
            var blueChannelArr = new byte[bitmap.Height, bitmap.Width];

            var firstPixelPtr = bitmapData.Scan0;

            var bitmapDataStride = bitmapData.Stride;
            var bitmapWidth = bitmap.Width;
            Parallel.For(0, bitmapData.Height, y =>
            {
                var row = new byte[bitmapData.Width * bytesPerPixel];
                var currentLine = firstPixelPtr + y * bitmapDataStride;

                Marshal.Copy(currentLine, row, 0, row.Length);

                for (var x = 0; x < bitmapWidth; x++)
                {
                    var currentPixelOffset = x * bytesPerPixel;
                    var blue = row[currentPixelOffset];
                    var green = row[currentPixelOffset + 1];
                    var red = row[currentPixelOffset + 2];
                    switch (_viewModel.ChannelSeparationMode)
                    {
                        case ChannelSeparationMode.Direct:
                            redChannelArr[y, x] = red;
                            greenChannelArr[y, x] = green;
                            blueChannelArr[y, x] = blue;
                            break;
                        case ChannelSeparationMode.SubtractOtherChannelAverage:
                            blueChannelArr[y, x] = (byte)Math.Max(0, blue - (red + green) / 2);
                            greenChannelArr[y, x] = (byte)Math.Max(0, green - (red + blue) / 2);
                            redChannelArr[y, x] = (byte)Math.Max(0, red - (green + blue) / 2);
                            break;
                        case ChannelSeparationMode.SubtractAverage:
                            var white = (red + green + blue) / 3;
                            blueChannelArr[y, x] = (byte)Math.Max(0, blue - white);
                            greenChannelArr[y, x] = (byte)Math.Max(0, green - white);
                            redChannelArr[y, x] = (byte)Math.Max(0, red - white);
                            break;
                        case ChannelSeparationMode.SubtractOtherChannels:
                            blueChannelArr[y, x] = (byte)Math.Max(0, blue - (red + green));
                            greenChannelArr[y, x] = (byte)Math.Max(0, green - (red + blue));
                            redChannelArr[y, x] = (byte)Math.Max(0, red - (green + blue));
                            break;
                        case ChannelSeparationMode.BlackAndWhiteByAverage:
                            byte white2 = (byte)((red + green + blue) / 3);
                            blueChannelArr[y, x] = white2;
                            greenChannelArr[y, x] =  white2;
                            redChannelArr[y, x] =  white2;
                            break;
                        case ChannelSeparationMode.BlackAndWhiteByMininum:
                            byte white3 = Math.Min(red, Math.Min(green, blue));
                            blueChannelArr[y, x] = white3;
                            greenChannelArr[y, x] =  white3;
                            redChannelArr[y, x] =  white3;
                            break;
                    }
                    // redChannelArr[y, x] = (byte)Math.Max(0, row[currentPixelOffset + 2] - (0.2*row[currentPixelOffset + 1] + 0.1*row[currentPixelOffset]));
                    // greenChannelArr[y, x] = (byte)Math.Max(0, row[currentPixelOffset + 1] - (0.2*row[currentPixelOffset + 2] + 0.1*row[currentPixelOffset]));
                    // blueChannelArr[y, x] = (byte)Math.Max(0, row[currentPixelOffset] - (0.2*row[currentPixelOffset + 2] + 0.1*row[currentPixelOffset + 1]));
                }
            });
            bitmap.UnlockBits(bitmapData);
            return (redChannelArr, greenChannelArr, blueChannelArr);
        }
    }

    public Bitmap DownscaleBitmap(Bitmap bitmap, int factor)
    {
        using (_timingCapture.CreateWatch("Downscale Bitmap"))
        {
            var newWidth = bitmap.Width / factor;
            var newHeight = bitmap.Height / factor;
            var resizedBitmap = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(resizedBitmap))
            {
                // Set high-quality interpolation mode
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // Draw the original bitmap onto the resized bitmap
                graphics.DrawImage(bitmap, 0, 0, newWidth, newHeight);
            }

            return resizedBitmap;
        }
    }

    public Bitmap TrimBitmap(Bitmap source)
    {
        using (_timingCapture.CreateWatch("Trim Bitmap"))
        {
            var left = (int)(source.Width * _viewModel.TrimLeft / 100f);
            var right = (int)(source.Width * _viewModel.TrimRight / 100f);
            var top = (int)(source.Height * _viewModel.TrimTop / 100f);
            var bottom = (int)(source.Height * _viewModel.TrimBottom / 100f);

            var cropRect = new Rectangle(left, top, source.Width - left - right, source.Height - top - bottom);

            var target = new Bitmap(cropRect.Width, cropRect.Height);

            using (var g = Graphics.FromImage(target))
            {
                g.DrawImage(source, new Rectangle(0, 0, target.Width, target.Height),
                    cropRect,
                    GraphicsUnit.Pixel);
            }

            return target;
        }
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        await _ledController.Connect();
    }

    private async void StartScan_OnClick(object? sender, RoutedEventArgs e)
    {
        await _ledController.RunScan();
    }
}
