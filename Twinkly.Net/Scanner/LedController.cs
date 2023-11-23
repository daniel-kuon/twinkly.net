using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DynamicData.Binding;
using ReactiveUI;
using Twinkly.Net;
using Twinkly.Net.DTOs.Enums;

namespace Scanner;

public class LedController : ReactiveObject
{
    private readonly LightControlViewModel _lightControlViewModel;
    private readonly ScanViewModel _scanViewModel;
    private TwinklyClient? _client;
    private CancellationTokenSource _cancellationTokenSource = new();
    private bool _scanRunning;
    private readonly List<ChannelProcessor> _channelProcessors;
    private readonly MainWindowViewModel _viewModel;
    private readonly ChannelProcessor _blueChannelProcessor;
    private readonly ChannelProcessor _greenChannelProcessor;
    private readonly ChannelProcessor _redChannelProcessor;

    public LedController(MainWindowViewModel viewModel,
        ChannelProcessor redChannelProcessor, ChannelProcessor greenChannelProcessor,
        ChannelProcessor blueChannelProcessor)
    {
        _lightControlViewModel = viewModel.LightControlViewModel;
        _scanViewModel = viewModel.ScanViewModel;
        _viewModel = viewModel;
        viewModel.WhenValueChanged(v => v.SelectedTab).Subscribe(TabChanged);
        _redChannelProcessor = redChannelProcessor;
        _greenChannelProcessor = greenChannelProcessor;
        _blueChannelProcessor = blueChannelProcessor;
        _lightControlViewModel.WhenAnyPropertyChanged().Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(_ => UpdateLeds());
        _scanViewModel.WhenPropertyChanged(m => m.SelectedScanRun.SelectedLed).Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(ActivateSingleLed);
        if (_lightControlViewModel.IpAddress != "")
            Connect().ConfigureAwait(false);
    }

    private void ActivateSingleLed(PropertyValue<ScanViewModel, LedScanResult> value)
    {
        var ledIndex = value.Value?.Index;

        if (ledIndex is null || _client is null)
            return;

        var nullBytes = _client.LedProfile == LedProfile.Rgb
            ? new byte[] { 0, 0, 0 }
            : new byte[] { 0, 0, 0, 0 };

        var whiteBytes = _client.LedProfile == LedProfile.Rgb
            ? new byte[] { 255, 255, 255 }
            : new byte[] { 255, 255, 255, 255 };

        var ledBytes = Enumerable.Range(0, _client.LedsCount)
            .Select(i => i == ledIndex ? whiteBytes : nullBytes).ToArray();

        _client.SendFrame(ledBytes).ConfigureAwait(false);
    }

    private void TabChanged(MainWindowTabItem tab)
    {
        if (tab == MainWindowTabItem.Scan)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }
        else if (tab == MainWindowTabItem.CameraConfig)
        {
            UpdateLeds();
        }
    }

    public async Task Connect()
    {
        if (!IPAddress.TryParse(_lightControlViewModel.IpAddress, out var ipAddress))
        {
            return;
        }

        var logger = new StringLogger<TwinklyClient>();
        _client = new TwinklyClient(ipAddress, logger, new HttpClient());
        await _client.Connect();
        _lightControlViewModel.IsConnected = true;
    }

    public async Task RunScan()
    {
        if (_client is null)
        {
            return;
        }

        _scanRunning = true;
        _cancellationTokenSource.Cancel();
        _scanViewModel.IsScanning = true;


        var images = new List<byte[][,]>();
        var requests = GenerateScanPatterns();

        foreach (var request in requests)
        {
            var redChannel = ProcessChannel(request.red, _redChannelProcessor);
            var greenChannel = ProcessChannel(request.green, _greenChannelProcessor);
            var blueChannel = ProcessChannel(request.blue, _blueChannelProcessor);

            RemoveAlwaysOnPixels(redChannel, greenChannel, blueChannel);

            images.Add(new[] { redChannel, greenChannel, blueChannel });
        }

        UpdateLeds();

        var ledScanResults = new List<LedScanResult>();

        for (var i = 0; i < _client.LedsCount; i++)
        {
            _scanViewModel.ScanProgress = (int)((float)i * 100 / _client.LedsCount);
            var ledIndexRepresentation = ConvertToBase3(i, requests.Length);
            var ledByteArrays = ledIndexRepresentation.Select((digit, index) => images[index][digit]).ToArray();
            var mergedByteArray = MergeChannelArraysToBitmap(ledByteArrays);



            ledScanResults.Add(new LedScanResult(i, ledByteArrays, mergedByteArray));
        }

        _scanViewModel.ScanRuns.Add(new ScanRun("", DateTime.Now,
            ledScanResults, new List<Bitmap>()));
        _scanViewModel.IsScanning = false;
        _scanRunning = false;
        _scanViewModel.ScanProgress = 0;
    }

    private byte[,] ProcessChannel(byte[][] channelData, ChannelProcessor processor)
    {
        if (_client is null)
        {
            return new byte[0, 0];
        }
        _client.SendFrame(channelData).Wait();
        Task.Delay(400).Wait();
        processor.ClearFrameCache();
        processor.AwaitFilledFrameCache().ConfigureAwait(false).GetAwaiter().GetResult();
        return processor.LastProcessedFrame;
    }

    private void RemoveAlwaysOnPixels(params byte[][,] channels)
    {
        var height = channels[0].GetLength(0);
        var width = channels[0].GetLength(1);
       Parallel.For(0, height, y =>
        {
            for (var x = 0; x < width; x++)
            {
                var allChannelsOn = true;
                foreach (var channel in channels)
                {
                    allChannelsOn &= channel[y, x] != 0;
                }

                if (allChannelsOn)
                {
                    foreach (var channel in channels)
                    {
                        channel[y, x] = 0;
                    }
                }
            }
        });
    }

    private (byte[][] red, byte[][] green, byte[][] blue)[] GenerateScanPatterns()
    {
        if (_client is null)
        {
            return Array.Empty<(byte[][] red, byte[][] green, byte[][] blue)>();
        }

        Byte[] red;
        Byte[] green;
        Byte[] blue;
        Byte[] off;
        Byte[] white;
        var scanFrameCount = (int)Math.Ceiling(Math.Log(_client.LedsCount, 3));

        if (_client.LedProfile == LedProfile.Rgb)
        {
            red = new byte[] { 255, 0, 0 };
            green = new byte[] { 0, 255, 0 };
            blue = new byte[] { 0, 0, 255 };
            off = new byte[] { 0, 0, 0 };
            white = new byte[] { 255, 255, 255 };
        }
        else
        {
            red = new byte[] { 0, 255, 0, 0 };
            green = new byte[] { 0, 0, 255, 0 };
            blue = new byte[] { 0, 0, 0, 255 };
            off = new byte[] { 0, 0, 0, 0 };
            white = new byte[] { 255, 255, 255, 255 };
        }

        var ledBase3Index = Enumerable.Range(0, _client.LedsCount)
            .Select(index => ConvertToBase3(index, scanFrameCount)).ToArray();
        return Enumerable.Range(0, scanFrameCount).Select(BuildFrame).ToArray();

        (byte[][] red, byte[][] green, byte[][] blue) BuildFrame(int frameIndex)
        {
            return (BuildChannelFrame(BaseColor.Red, frameIndex),
                BuildChannelFrame(BaseColor.Green, frameIndex),
                BuildChannelFrame(BaseColor.Blue, frameIndex));
        }

        byte[][] BuildChannelFrame(BaseColor color, int frameIndex)
        {
            // var bytes = color switch
            // {
            //     BaseColor.Red => red,
            //     BaseColor.Green => green,
            //     BaseColor.Blue => blue,
            //     _ => throw new ArgumentOutOfRangeException()
            // };
            //
            // bytes = white;

            var colorIndex = color switch
            {
                BaseColor.Red => 0,
                BaseColor.Green => 1,
                BaseColor.Blue => 2,
                _ => throw new ArgumentOutOfRangeException()
            };

            return ledBase3Index.Select(led =>
                led[frameIndex] == colorIndex ? white : off).ToArray();
        }
    }

    private byte[,] MergeChannelArraysToBitmap(byte[][,] ledImages)
    {
        var height = ledImages[0].GetLength(0);
        var width = ledImages[0].GetLength(1);
        var mergedArray = new byte[height, width];
        if (_scanViewModel.ImageMergeMode == ImageMergeMode.SummedAverage)
        {
            Parallel.For(0, height, y =>
            {
                for (var x = 0; x < width; x++)
                {
                    var sum = 0;
                    foreach (var image in ledImages)
                    {
                        sum += image[y, x];
                    }

                    mergedArray[y, x] = (byte)(sum / ledImages.Length);
                }
            });
        }
        else if (_scanViewModel.ImageMergeMode == ImageMergeMode.Binary)
        {
            Parallel.For(0, height, y =>
            {
                for (var x = 0; x < width; x++)
                {
                    mergedArray[y, x] = 255;
                    foreach (var image in ledImages)
                    {
                        if (image[y, x] == 0)
                        {
                            mergedArray[y, x] = 0;
                            break;
                        }
                    }

                }
            });
        // } else if (_scanViewModel.ImageMergeMode == ImageMergeMode.Multiply)
        // {
        //     var lightMaps = new byte[ledImages.Length, height, width];
        //     for (var i = 0; i < ledImages.Length; i++)
        //     {
        //         var image = ledImages[i];
        //
        //     }
    }

        return mergedArray;

    }

int[] ConvertToBase3(int number, int scanFrameCount)
{
    var digits = new List<int>();

    while (number > 0)
    {
        var remainder = number % 3;
        digits.Add(remainder);
        number /= 3;
    }

    digits.AddRange(Enumerable.Repeat(0, scanFrameCount - digits.Count));

    return digits.ToArray();
}


private async Task UpdateLeds()
{
    if (_client is null || _viewModel.SelectedTab == MainWindowTabItem.Scan)
    {
        return;
    }

    if (_scanRunning) return;
    _cancellationTokenSource.Cancel();
    _cancellationTokenSource.Dispose();
    _cancellationTokenSource = new CancellationTokenSource();
    switch (_lightControlViewModel.Mode)
    {
        case LightControlMode.BaseColor:
            await SetBaseColor();
            break;
        case LightControlMode.CustomColor:
            await SetCustomColor();
            break;
        case LightControlMode.ColorChange:
            await StartColorChange();
            break;
        case LightControlMode.LedScanPattern:
            await CycleLedScanPatterns();
            break;
        default:
            throw new ArgumentOutOfRangeException();
    }
}

private async Task CycleLedScanPatterns()
{
    if (_client is null)
    {
        return;
    }

    var frames = GenerateScanPatterns();

    var cancellationTokenSource = _cancellationTokenSource;
    while (!cancellationTokenSource.IsCancellationRequested)
    {
        foreach (var frame in frames.SelectMany(f => new[] { f.red, f.green, f.blue }))
        {
            await _client.SendFrame(frame);
            await DelayWithoutException(_lightControlViewModel.ColorChangeSpeed, cancellationTokenSource.Token);
        }
    }
}

private async Task StartColorChange()
{
    var color = BaseColor.Red;
    var cancellationTokenSource = _cancellationTokenSource;
    while (!cancellationTokenSource.IsCancellationRequested)
    {
        switch (color)
        {
            case BaseColor.Red:
                await SetCustomColor(255, 0, 0);
                break;
            case BaseColor.Green:
                await SetCustomColor(0, 255, 0);
                break;
            case BaseColor.Blue:
                await SetCustomColor(0, 0, 255);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        color = color switch
        {
            BaseColor.Red => BaseColor.Green,
            BaseColor.Green => BaseColor.Blue,
            BaseColor.Blue => BaseColor.Red,
            _ => throw new ArgumentOutOfRangeException()
        };
        await DelayWithoutException(_lightControlViewModel.ColorChangeSpeed, cancellationTokenSource.Token);
    }
}

private async Task DelayWithoutException(int delay, CancellationToken cancellationToken)
{
    try
    {
        await Task.Delay(delay, cancellationToken);
    }
    catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
    {
    }
}

private async Task SetCustomColor()
{
    await SetCustomColor(_lightControlViewModel.Red, _lightControlViewModel.Green, _lightControlViewModel.Blue);
}

private async Task SetCustomColor(int red, int green, int blue)
{
    if (_client is null)
    {
        return;
    }

    await _client.SetRgbColor(red, green, blue);
    await _client.SetBrightness(_lightControlViewModel.Brightness);
}


private async Task SetBaseColor()
{
    switch (_lightControlViewModel.BaseColor)
    {
        case BaseColor.Red:
            await SetCustomColor(255, 0, 0);
            break;
        case BaseColor.Green:
            await SetCustomColor(0, 255, 0);
            break;
        case BaseColor.Blue:
            await SetCustomColor(0, 0, 255);
            break;
        default:
            throw new ArgumentOutOfRangeException();
    }
}

public void Dispose()
{
    _cancellationTokenSource.Cancel();
    _cancellationTokenSource.Dispose();
}

}
