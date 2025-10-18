using System;
using System.Buffers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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
    private readonly LedController _ledController;

    private readonly ChannelProcessor _channelProcessor;
    private readonly TimingCapture _redTimingCapture;

    private readonly TimingCapture _timingCapture = new(SampleSize);
    private readonly MainWindowViewModel _viewModel;
    private ObservableCaptureDevice? _activeCamera;

    private byte? _lowerThresholdBeforeBaseImageCapture;
    private byte? _upperThresholdBeforeBaseImageCapture;


    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        _redTimingCapture = new TimingCapture(SampleSize, _timingCapture);
        DataContext = _viewModel;

        _redTimingCapture.OnTimingStringUpdated += (sender, timings) => _viewModel.RedTimings = timings;
        _channelProcessor = new ChannelProcessor(_viewModel, _redTimingCapture);
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
            if (_activeCamera is { } activeCamera) activeCamera.Dispose();

            if (_viewModel.SelectedCamera is null || characteristic is null) return;

            _activeCamera = await _viewModel.SelectedCamera.AsObservableAsync(characteristic);
            _activeCamera.Subscribe(ActiveCameraOnNewFrame);
            await _activeCamera.StartAsync();

            CaptureBaseImage();
        });

        _viewModel.WhenAnyPropertyChanged(nameof(MainWindowViewModel.TrimLeft), nameof(MainWindowViewModel.TrimRight),
            nameof(MainWindowViewModel.TrimTop), nameof(MainWindowViewModel.TrimBottom)
            , nameof(MainWindowViewModel.SelectedCharacteristic), nameof(MainWindowViewModel.ScaleDownFactor),
            nameof(MainWindowViewModel.ChannelSeparationMode)
        ).Subscribe(_ => { ClearFrameCaches(); });

        _viewModel.WhenAnyPropertyChanged(nameof(MainWindowViewModel.TrimLeft), nameof(MainWindowViewModel.TrimRight),
            nameof(MainWindowViewModel.TrimTop), nameof(MainWindowViewModel.TrimBottom)
            , nameof(MainWindowViewModel.SelectedCharacteristic), nameof(MainWindowViewModel.ScaleDownFactor)
        ).Throttle(TimeSpan.FromMilliseconds(1000)).Subscribe(_ => { CaptureBaseImage(); });

        var settings = Settings.Load();
        settings.SetToViewModel(_viewModel);

        _ledController =
            new LedController(_viewModel, _channelProcessor);

        Closing += (_, _) => PrepareClosing();
    }

    private async void CaptureBaseImage()
    {
        _lowerThresholdBeforeBaseImageCapture ??= _viewModel.LowerThreshold;
        _upperThresholdBeforeBaseImageCapture ??= _viewModel.UpperThreshold;
        _viewModel.LowerThreshold = 0;
        _viewModel.UpperThreshold = 255;
        _viewModel.LightControlViewModel.Activated = false;
        await Task.Delay(300);
        ClearFrameCaches();
        await _channelProcessor.AwaitFilledFrameCache();
        _channelProcessor.BaseFrame = _channelProcessor.LastProcessedFrame;
        _viewModel.LightControlViewModel.Activated = true;
        _viewModel.LowerThreshold = _lowerThresholdBeforeBaseImageCapture.Value;
        _viewModel.UpperThreshold = _upperThresholdBeforeBaseImageCapture.Value;

    }

    private void ClearFrameCaches()
    {
        _channelProcessor.ClearFrameCache();
        _channelProcessor.BaseFrame = null;
    }

    protected void PrepareClosing()
    {
        Settings.FromViewModel(_viewModel).Save();
        _ledController.Dispose();
    }

    private async void ActiveCameraOnNewFrame(PixelBufferScope bufferScope)
    {
        Bitmap bitmap;
        using (_timingCapture.CreateWatch("Create Bitmap"))
        {
            bitmap = new Bitmap(new MemoryStream(bufferScope.Buffer.ExtractImage()));
        }

        if (_viewModel.ScaleDownFactor > 1) bitmap = DownscaleBitmap(bitmap, _viewModel.ScaleDownFactor);

        var channelArr = ConvertToColorChannelArray(bitmap);
        // var copy = channelArr.Clone() as byte[,];

        _viewModel.LastInputFrame = ImageByteArrayConverter.ConvertToImage(channelArr);
        _viewModel.LastRawFrame = await Task.Run(() => _channelProcessor.ProcessChanel(channelArr));

        // _viewModel.OverlayChannel = ApplyOverlay(copy, _channelProcessor.LastProcessedFrame);
    }
    private IImage ApplyOverlay(byte[,] baseImage, byte[,] overlay)
    {
        // Verify dimensions
        if (baseImage.GetLength(0) != overlay.GetLength(0) || baseImage.GetLength(1) != overlay.GetLength(1))
        {
            throw new ArgumentException("Base image and overlay must have the same dimensions.");
        }

        byte[,] resultImage = new byte[baseImage.GetLength(0), baseImage.GetLength(1)];

        for (int y = 0; y < baseImage.GetLength(0); y++)
        {
            for (int x = 0; x < baseImage.GetLength(1); x++)
            {
                byte basePixel = baseImage[y, x];
                byte overlayPixel = overlay[y, x];

                // Convert the brightness (assumed to be overlayPixel value) to alpha
                byte alpha = overlayPixel;

                // apply the overlay on the base pixel
                float src = alpha / 255f;
                float dest = 1f - src;

                byte a = (byte)(src + basePixel / 255f * dest);
                byte r = (byte)((255 * src) + basePixel / 255f * dest);

                resultImage[y, x] = (byte)(((a & 0xFF) << 24) | ((r & 0xFF) << 16) | ((r & 0xFF) << 8) | (r & 0xFF));  // ARGB
            }
        }

        return ImageByteArrayConverter.ConvertToImage(resultImage);
    }

    private (byte[,] redChannelArr, byte[,] greenChannelArr, byte[,] blueChannelArr) ConvertToColorChannelArrays(
        Bitmap bitmap)
    {
        using (_timingCapture.CreateWatch("Convert To Channel Arrays"))
        {
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
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
                            var white2 = (byte)((red + green + blue) / 3);
                            blueChannelArr[y, x] = white2;
                            greenChannelArr[y, x] = white2;
                            redChannelArr[y, x] = white2;
                            break;
                        case ChannelSeparationMode.BlackAndWhiteByMininum:
                            var white3 = Math.Min(red, Math.Min(green, blue));
                            blueChannelArr[y, x] = white3;
                            greenChannelArr[y, x] = white3;
                            redChannelArr[y, x] = white3;
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

    private byte[,] ConvertToColorChannelArray(
        Bitmap bitmap)
    {
        using (_timingCapture.CreateWatch("Convert To Channel Array"))
        {
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat);
            var bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            var firstPixelPtr = bitmapData.Scan0;

            var bitmapDataStride = bitmapData.Stride;

            int left = 0;
            int right = 0;
            int top = 0;
            int bottom = 0;
            if (_viewModel.TrimImage)
            {
                left = (int)(bitmap.Width * _viewModel.TrimLeft / 100f);
                right = (int)(bitmap.Width * _viewModel.TrimRight / 100f);
                top = (int)(bitmap.Height * _viewModel.TrimTop / 100f);
                bottom = (int)(bitmap.Height * _viewModel.TrimBottom / 100f);
            }
            var width = bitmapData.Width - left - right;
            var height = bitmapData.Height - top - bottom;

            var channelArr = new byte[height, width];
            Parallel.For(0, height, y =>
            {
                var row = new byte[width * bytesPerPixel];
                var currentLine = firstPixelPtr + (y + top) * bitmapDataStride + left * bytesPerPixel;

                Marshal.Copy(currentLine, row, 0, row.Length);

                for (var x = 0; x < width; x++)
                {
                    var currentPixelOffset = x * bytesPerPixel;
                    var blue = row[currentPixelOffset];
                    var green = row[currentPixelOffset + 1];
                    var red = row[currentPixelOffset + 2];
                    switch (_viewModel.ChannelSeparationMode)
                    {
                        case ChannelSeparationMode.BlackAndWhiteByAverage:
                            channelArr[y, x] = (byte)((red + green + blue) / 3);
                            break;
                        case ChannelSeparationMode.BlackAndWhiteByMininum:
                            channelArr[y, x] = Math.Min(red, Math.Min(green, blue));
                            break;
                    }
                }
            });
            bitmap.UnlockBits(bitmapData);
            return (channelArr);
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

    private async void CaptureBaseImage_OnClick(object? sender, RoutedEventArgs e)
    {
        CaptureBaseImage();
    }

    private async void Connect_OnClick(object? sender, RoutedEventArgs e)
    {
        await _ledController.Connect();
    }

    private void StartScan_OnClick(object? sender, RoutedEventArgs e)
    {
        Task.Run(() => _ledController.RunScan());
    }
}
