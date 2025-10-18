using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using DynamicData.Binding;

namespace Scanner;

public class ChannelProcessor
{
    private readonly FixedSizedList<byte[,]> _frameCache = new(10);
    private readonly TimingCapture _timingCapture;
    private readonly MainWindowViewModel _viewModel;

    public ChannelProcessor(MainWindowViewModel viewModel, TimingCapture timingCapture)
    {
        _viewModel = viewModel;
        _timingCapture = timingCapture;
        _viewModel.WhenValueChanged(v => v.TimeAverageSampleSize, false).Subscribe(v => _frameCache.Limit = v);
    }


    public IImage ProcessChanel(byte[,] channelArr)
    {
        var _ = _timingCapture.CreateWatch("Process Channel");
        _frameCache.Add(channelArr);

        if (_viewModel.TimeAverage) channelArr = CalculateAverageFrame();

        if (_viewModel.SubtractBaseImage && BaseFrame != null) SubtractBaseFrame(channelArr);

        if (_viewModel.Blur) channelArr = BlurChannelAndApplyTreshold(channelArr);
        else if (_viewModel.LowerThreshold > 0 || _viewModel.UpperThreshold < 255) ApplyThreshold(channelArr);

        OnFrameProcessed?.Invoke(this, channelArr);

        LastProcessedFrame = channelArr;

        return ConvertToBitmap(channelArr);
    }

    private void SubtractBaseFrame(byte[,] channelArr)
    {
        using var _ = _timingCapture.CreateWatch("Subtract Base Frame");
        int height = channelArr.GetLength(0);
        int width = channelArr.GetLength(1);
        var baseFrame = BaseFrame;
        if (baseFrame == null) return;
        Parallel.For(0, height, y =>
        {
            for (var x = 0; x < width; x++)
            {
                channelArr[y, x] = (byte)Math.Max(0, channelArr[y, x] - baseFrame[y, x]);
            }
        });
    }

    public byte[,] LastProcessedFrame { get; private set; }

    public byte[,]? BaseFrame { get; set; }

    public event EventHandler<byte[,]>? OnFrameProcessed;

    private void BlurChannel(byte[,] channelArr)
    {
        using var _ = _timingCapture.CreateWatch("Blur Channel");
        int height = channelArr.GetLength(0);
        int width = channelArr.GetLength(1);
        Parallel.For(0, height, y =>
        {
            for (var x = 0; x < width; x++)
            {
                var sum = 0;
                var count = 0;
                for (var y2 = y - _viewModel.BlurSize; y2 <= y + _viewModel.BlurSize; y2++)
                for (var x2 = x - _viewModel.BlurSize; x2 <= x + _viewModel.BlurSize; x2++)
                {
                    if (y2 < 0 || y2 >= height || x2 < 0 || x2 >= width) continue;

                    sum += channelArr[y2, x2];
                    count++;
                }

                channelArr[y, x] = (byte)(sum / count);
            }
        });
    }

    private byte[,] BlurChannelAndApplyTreshold(byte[,] channelArr)
    {
        using var _ = _timingCapture.CreateWatch("Blur Channel");
        int height = channelArr.GetLength(0);
        int width = channelArr.GetLength(1);
        var target = new byte[height, width];
        Parallel.For(0, height, y =>
        {
            for (var x = 0; x < width; x++)
            {
                if (channelArr[y, x] < _viewModel.LowerThreshold || channelArr[y, x] > _viewModel.UpperThreshold)
                {
                    continue;
                }

                var sum = 0;
                var count = 0;
                for (var y2 = y - _viewModel.BlurSize; y2 <= y + _viewModel.BlurSize; y2++)
                for (var x2 = x - _viewModel.BlurSize; x2 <= x + _viewModel.BlurSize; x2++)
                {
                    if (y2 < 0 || y2 >= height || x2 < 0 || x2 >= width) continue;

                    sum += channelArr[y2, x2];
                    count++;
                }

                target[y, x] = (byte)(sum / count);
            }
        });
        return target;
    }

    public void ApplyThreshold(byte[,] channelArr)
    {
        using var _ = _timingCapture.CreateWatch("Apply Threshold");
        int height = channelArr.GetLength(0);
        int width = channelArr.GetLength(1);
        Parallel.For(0, height, y =>
        {
            for (var x = 0; x < width; x++)
            {
                if (channelArr[y, x] < _viewModel.LowerThreshold || channelArr[y, x] > _viewModel.UpperThreshold)
                    channelArr[y, x] = 0;
            }
        });
    }

    private byte[,] CalculateAverageFrame()
    {
        var frameCacheList = _frameCache.List.ToList();
        var latestFrame = frameCacheList.Last();
        var height = latestFrame.GetLength(0);
        var width = latestFrame.GetLength(1);
        frameCacheList.RemoveAll(frame => frame.GetLength(0) != height || frame.GetLength(1) != width);
        var output = new byte[height, width];
        Parallel.For(0, height, y =>
        {
            for (var x = 0; x < width; x++)
            {
                var sum = 0;
                foreach (var frame in frameCacheList) sum += frame[y, x];

                output[y, x] = (byte)(sum / frameCacheList.Count);
            }
        });

        return output;
    }

    private IImage ConvertToBitmap(byte[,] channelArr)
    {
        using var _ = _timingCapture.CreateWatch("Convert To Bitmap");
        return ImageByteArrayConverter.ConvertToImage(channelArr);
    }

    public void ClearFrameCache()
    {
        _frameCache.List.Clear();
    }

    public Task AwaitFilledFrameCache()
    {
        if (_frameCache.List.Count >= _viewModel.TimeAverageSampleSize) return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

        EventHandler<byte[,]>? handler = null;
        handler = (_, _) =>
        {
            if (_frameCache.List.Count >= _viewModel.TimeAverageSampleSize)
            {
                OnFrameProcessed -= handler; // Unsubscribe from the event
                tcs.SetResult(true); // Mark the Task as complete
            }
        };

        OnFrameProcessed += handler;
        return tcs.Task;
    }
}
