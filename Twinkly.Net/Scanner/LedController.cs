using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly MainWindowViewModel _viewModel;
    private readonly ChannelProcessor _channelProcessor;
    private byte[][][][]? _scanPatternFrames;
    private readonly TimingCapture _timingCapture = new();

    public LedController(MainWindowViewModel viewModel,
        ChannelProcessor channelProcessor)
    {
        _lightControlViewModel = viewModel.LightControlViewModel;
        _scanViewModel = viewModel.ScanViewModel;
        _viewModel = viewModel;
        viewModel.WhenValueChanged(v => v.SelectedTab).Subscribe(TabChanged);
        _channelProcessor = channelProcessor;
        _lightControlViewModel.WhenAnyPropertyChanged().Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(_ => UpdateLeds());
        _scanViewModel.WhenPropertyChanged(m => m.SelectedScanRun).Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(_ => UpdateLeds());
        _lightControlViewModel.WhenAnyPropertyChanged(nameof(_lightControlViewModel.Red),
                nameof(_lightControlViewModel.Green), nameof(_lightControlViewModel.Blue),
                nameof(_lightControlViewModel.White))
            .Subscribe(_ => _scanPatternFrames = null);
        _scanViewModel.WhenPropertyChanged(m => m.SelectedScanRun!.SelectedLed).Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(ActivateSingleLed);
        _scanViewModel.WhenPropertyChanged(m => m.SelectedScanRun!.SelectedLed!.SelectedLedScanPatternFrame)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(ActivateLedPattern);
        if (_lightControlViewModel.IpAddress != "")
            Connect().ConfigureAwait(false);
        _timingCapture.OnTimingStringUpdated += (_, timingString) => _lightControlViewModel.Timings = timingString;
    }

    private void ActivateSingleLed(PropertyValue<ScanViewModel, LedScanResult?> value)
    {
        var ledIndex = value.Value?.Index;

        if (ledIndex is null || _client is null)
            return;

        byte[] nullBytes = _client.LedByteMode == LedByteMode.RgbAww
            ? [0, 0, 0]
            : [0, 0, 0, 0 ];

        byte[] whiteBytes = _client.LedByteMode == LedByteMode.RgbAww
                                ? [255, 255, 255]
                                : [255, 255, 255, 255];

        var ledBytes = Enumerable.Range(0, _client.LedsCount)
            .Select(i => i == ledIndex ? whiteBytes : nullBytes).ToArray();

        _client.SendFrame(ledBytes).ConfigureAwait(false);
    }

    private void ActivateLedPattern(PropertyValue<ScanViewModel, byte[][]?> value)
    {
        var pattern = value.Value;

        if (pattern is null || _client is null)
            return;

        _client.SendUdpFrame(pattern).ConfigureAwait(false);
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
        _client = await TwinklyClient.Create(ipAddress, logger, new HttpClient());
        _lightControlViewModel.RgbwMode = _client.LedProfile == LedProfile.Rgbw;
        _lightControlViewModel.IsConnected = true;
    }

    public async void RunScan()
    {
        if (_client is null)
        {
            return;
        }

        _scanRunning = true;
        await _cancellationTokenSource.CancelAsync();
        _scanViewModel.IsScanning = true;


        var images = new List<byte[][,]>();
        var scanPatternFrames = _scanPatternFrames ??= GenerateScanPatterns();

        foreach (var request in scanPatternFrames)
        {
            var redChannel = await ProcessChannel(request[0], _channelProcessor);
            var greenChannel = await ProcessChannel(request[1], _channelProcessor);
            var blueChannel = await ProcessChannel(request[2], _channelProcessor);

            RemoveAlwaysOnPixels(redChannel, greenChannel, blueChannel);

            images.Add([redChannel, greenChannel, blueChannel]);
        }

        UpdateLeds();

        var ledScanResults = new List<LedScanResult>();


        Parallel.For(0, _client.LedsCount, i =>
        {
            var ledIndexRepresentation = ConvertToBase3(i, scanPatternFrames.Length);
            var ledByteArrays = ledIndexRepresentation.Select((digit, index) => images[index][digit]).ToArray();
            var ledScanPatternFrames = ledIndexRepresentation.Select((digit, index) => scanPatternFrames[index][digit])
                .ToArray();
            var mergedByteArray = MergeChannelArraysToBitmap(ledByteArrays);
            var center = ClusterFinder.GetLargestClusterCenter(mergedByteArray);

            ledScanResults.Add(new LedScanResult(i, ledByteArrays, mergedByteArray, center, ledScanPatternFrames));
            _scanViewModel.ScanProgress = (int)((float)ledScanResults.Count * 100 / _client.LedsCount);
        });

        ledScanResults.Sort((a, b) => a.Index.CompareTo(b.Index));

        _scanViewModel.ScanRuns.Add(new ScanRun("", DateTime.Now,
            ledScanResults,
            []));
        _scanViewModel.IsScanning = false;
        _scanViewModel.SelectedScanRun = _scanViewModel.ScanRuns.Last();
        _scanViewModel.SelectedScanRun.SelectedLed = _scanViewModel.SelectedScanRun.Leds.First();
        _scanRunning = false;
        _scanViewModel.ScanProgress = 0;
        _lightControlViewModel.Mode = LightControlMode.Positions;
    }

    private async Task<byte[,]> ProcessChannel(byte[][] channelData, ChannelProcessor processor)
    {
        if (_client is null)
        {
            return new byte[0, 0];
        }

        await _client.SendFrame(channelData);
        await Task.Delay(200);
        processor.ClearFrameCache();
        await processor.AwaitFilledFrameCache();
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

    private byte[][][][] GenerateScanPatterns()
    {
        if (_client is null)
        {
            return [];
        }

        Byte[] off;
        Byte[] white;
        var scanFrameCount = (int)Math.Ceiling(Math.Log(_client.LedsCount, 3));

        if (_client.LedByteMode == LedByteMode.RgbAww)
        {
            off = [0, 0, 0];
            white = [_lightControlViewModel.Red, _lightControlViewModel.Green, _lightControlViewModel.Blue];
        }
        else
        {
            off = [0, 0, 0, 0];
            white =
            [
                _lightControlViewModel.White, _lightControlViewModel.Red, _lightControlViewModel.Green,
                _lightControlViewModel.Blue
            ];
        }

        var ledBase3Index = Enumerable.Range(0, _client.LedsCount)
            .Select(index => ConvertToBase3(index, scanFrameCount)).ToArray();
        return Enumerable.Range(0, scanFrameCount).Select(BuildFrame).ToArray();

        byte[][][] BuildFrame(int frameIndex)
        {
            return
            [
                BuildChannelFrame(BaseColor.Red, frameIndex),
                BuildChannelFrame(BaseColor.Green, frameIndex),
                BuildChannelFrame(BaseColor.Blue, frameIndex)
            ];
        }

        byte[][] BuildChannelFrame(BaseColor color, int frameIndex)
        {
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
                    foreach (var image in ledImages)
                    {
                        if (image[y, x] == 0)
                        {
                            mergedArray[y, x] = 0;
                            goto ContinueOuterLoop;
                        }
                    }

                    mergedArray[y, x] = 255;

                    ContinueOuterLoop: ;
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


    private Task UpdateLeds()
    {
        if (_client is null || _viewModel.SelectedTab == MainWindowTabItem.Scan || _scanRunning)
        {
            return Task.CompletedTask;
        }

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        if (!_lightControlViewModel.Activated)
        {
            return SetCustomColor(0, 0, 0, 0);
        }

        switch (_lightControlViewModel.Mode)
        {
            case LightControlMode.BaseColor:
                return SetBaseColor();
            case LightControlMode.CustomColor:
                return SetCustomColor();
            case LightControlMode.ColorChange:
                return StartColorChange();
            case LightControlMode.LedScanPattern:
                return CycleLedScanPatterns();
            case LightControlMode.Positions:
                return SetPositionColors();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private Task SetPositionColors()
    {
        var leds = _scanViewModel.SelectedScanRun?.Leds;

        if (leds is null || _client is null)
        {
            return Task.CompletedTask;
        }

        var bytes = new byte[_client.LedsCount][];

        var minX = leds.Min(l => l.Center?.Left) ?? 0;
        var maxX = leds.Max(l => l.Center?.Left) ?? 0;
        var minY = leds.Min(l => l.Center?.Top) ?? 0;
        var maxY = leds.Max(l => l.Center?.Top) ?? 0;

        foreach (var led in leds)
        {
            var x = led.Center?.Left ?? 0;
            var y = led.Center?.Top ?? 0;
            var red = (byte)(255 * (x - minX) / (maxX - minX));
            var green = (byte)(255 * (y - minY) / (maxY - minY));
            byte blue = 0;
            if (_client.LedByteMode == LedByteMode.RgbAww)
            {
                bytes[led.Index] = [red, green, blue];
            }
            else
            {
                bytes[led.Index] = [(byte)0, red, green, blue];
            }
        }

        return _client.SendFrame(bytes);
    }

    private async Task CycleLedScanPatterns()
    {
        if (_client is null)
        {
            return;
        }

        var cancellationTokenSource = _cancellationTokenSource;
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var scanPatternFrames = _scanPatternFrames ??= GenerateScanPatterns();
            foreach (var frame in scanPatternFrames.SelectMany(f => f))
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
                    await SetCustomColor(255, 0, 0, 0);
                    break;
                case BaseColor.Green:
                    await SetCustomColor(0, 255, 0, 0);
                    break;
                case BaseColor.Blue:
                    await SetCustomColor(0, 0, 255, 0);
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
        await SetCustomColor(_lightControlViewModel.Red, _lightControlViewModel.Green, _lightControlViewModel.Blue,
            _lightControlViewModel.White);
    }

    private async Task SetCustomColor(byte red, byte green, byte blue, byte white)
    {
        using var _ =_timingCapture.CreateWatch("SetCustomColor");
        if (_client is null)
        {
            return;
        }

        var frameCreationWatch = _timingCapture.CreateWatch("FrameCreation");

        var ledColor = _client.LedByteMode == LedByteMode.Rgbw
            ? [white, red, green, blue]
            : new[] { red, green, blue };

        // for (var i = 0; i < ledColor.Length; i++)
        // {
        //     ledColor[i] = (byte)(_lightControlViewModel.Brightness * ledColor[i] / 255m);
        // }

        // var frame = Enumerable.Repeat(ledColor, _client.LedsCount).ToArray();
        //
        // frameCreationWatch.Dispose();
        //
        // await _client.SendFrame(frame);

        if (_client.LedByteMode == LedByteMode.RgbAww)
        {
            await _client.SetRgbColor(red, green, blue);
        }
        else
        {
            await _client.SetRgbwColor(red, green, blue, white);
        }
        //
        // await _client.SetBrightness(_lightControlViewModel.Brightness);
    }


    private async Task SetBaseColor()
    {
        switch (_lightControlViewModel.BaseColor)
        {
            case BaseColor.Red:
                await SetCustomColor(255, 0, 0, 0);
                break;
            case BaseColor.Green:
                await SetCustomColor(0, 255, 0, 0);
                break;
            case BaseColor.Blue:
                await SetCustomColor(0, 0, 255, 0);
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

public class ClusterFinder
{
    private static readonly int[] dr = [-1, 0, 1, 0];
    private static readonly int[] dc = [0, 1, 0, -1];

    public static (double Top, double Left)? GetLargestClusterCenter(byte[,] grid)
    {
        var rows = grid.GetLength(0);
        var cols = grid.GetLength(1);
        var visited = new bool[rows, cols];

        int maxClusterSize = 0;
        List<(int, int)>? maxClusterCells = null;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (grid[i, j] == 255 && !visited[i, j])
                {
                    var (clusterSize, cells) = ExploreCluster(i, j);
                    if (clusterSize > maxClusterSize)
                    {
                        maxClusterSize = clusterSize;
                        maxClusterCells = cells;
                    }
                }
            }
        }

        if (maxClusterCells == null)
        {
            return null;
        }

        var yCenter = maxClusterCells.Average(c => c.Item1) / rows;
        var xCenter = maxClusterCells.Average(c => c.Item2) / cols;
        return (yCenter, xCenter);

        (int ClusterSize, List<(int, int)> Cells) ExploreCluster(int row, int col)
        {
            visited[row, col] = true;
            int clusterSize = 1;
            var cells = new List<(int, int)> { (row, col) };

            for (int i = 0; i < 4; i++)
            {
                int newRow = row + dr[i];
                int newCol = col + dc[i];

                if (IsValid(newRow, newCol) && grid[newRow, newCol] == 255 && !visited[newRow, newCol])
                {
                    var (size, coordinates) = ExploreCluster(newRow, newCol);
                    clusterSize += size;
                    cells.AddRange(coordinates);
                }
            }

            return (clusterSize, cells);
        }

        bool IsValid(int row, int col)
        {
            return row >= 0 && col >= 0 && row < rows && col < cols;
        }
    }
}
