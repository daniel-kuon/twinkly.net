using System;
using System.Collections.ObjectModel;
using Avalonia.Media;
using FlashCap;
using OpenCvSharp;
using ReactiveUI;

namespace Scanner;

public class MainWindowViewModel : ReactiveObject
{
    private IImage? _blueChannel;
    private string _blueTimings = "";
    private IImage? _greenChannel;
    private string _greenTimings = "";
    private IImage? _redChannel;
    private string _redTimings = "";

    private CaptureDeviceDescriptor? _selectedCamera;
    private VideoCharacteristics? _selectedCharacteristic;

    private bool _timeAverage;
    private int _timeAverageSampleSize = 10;
    private float _trimBottom;
    private bool _trimImage;
    private float _trimLeft;
    private float _trimRight;
    private float _trimTop;
    private byte _lowerThreshold;
    private byte _upperThreshold;
    private int _scaleDownFactor = 1;
    private bool _blur;
    private int _blurSize = 3;
    private ChannelSeparationMode _channelSeparationMode;

    public ObservableCollection<CaptureDeviceDescriptor> CameraList { get; } = new();
    public ObservableCollection<VideoCharacteristics> CharacteristicList { get; } = new();

    public CaptureDeviceDescriptor? SelectedCamera
    {
        get => _selectedCamera;
        set => this.RaiseAndSetIfChanged(ref _selectedCamera, value);
    }

    public VideoCharacteristics? SelectedCharacteristic
    {
        get => _selectedCharacteristic;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCharacteristic, value);
            this.RaisePropertyChanged(nameof(ScaledDownResolution));
        }
    }

    public IImage? RedChannel
    {
        get => _redChannel;
        set => this.RaiseAndSetIfChanged(ref _redChannel, value);
    }

    public IImage? GreenChannel
    {
        get => _greenChannel;
        set => this.RaiseAndSetIfChanged(ref _greenChannel, value);
    }

    public IImage? BlueChannel
    {
        get => _blueChannel;
        set => this.RaiseAndSetIfChanged(ref _blueChannel, value);
    }

    public bool TimeAverage
    {
        get => _timeAverage;
        set => this.RaiseAndSetIfChanged(ref _timeAverage, value);
    }

    public int TimeAverageSampleSize
    {
        get => _timeAverageSampleSize;
        set => this.RaiseAndSetIfChanged(ref _timeAverageSampleSize, value);
    }

    public bool Blur
    {
        get => _blur;
        set => this.RaiseAndSetIfChanged(ref _blur, value);
    }

    public int BlurSize
    {
        get => _blurSize;
        set => this.RaiseAndSetIfChanged(ref _blurSize, value);
    }

    public bool TrimImage
    {
        get => _trimImage;
        set => this.RaiseAndSetIfChanged(ref _trimImage, value);
    }

    public float TrimRight
    {
        get => _trimRight;
        set
        {
            this.RaiseAndSetIfChanged(ref _trimRight, (float)Math.Round(value, 1));
            this.RaisePropertyChanged(nameof(TrimLeftMax));
        }
    }

    public float TrimRightMax => 99 - TrimLeft;

    public float TrimLeft
    {
        get => _trimLeft;
        set
        {
            this.RaiseAndSetIfChanged(ref _trimLeft, (float)Math.Round(value, 1));
            this.RaisePropertyChanged(nameof(TrimRightMax));
        }
    }

    public float TrimLeftMax => 99 - TrimRight;

    public float TrimTop
    {
        get => _trimTop;
        set
        {
            this.RaiseAndSetIfChanged(ref _trimTop, (float)Math.Round(value, 1));
            this.RaisePropertyChanged(nameof(TrimBottomMax));
        }
    }

    public float TrimTopMax => 99 - TrimBottom;

    public float TrimBottom
    {
        get => _trimBottom;
        set
        {
            this.RaiseAndSetIfChanged(ref _trimBottom, (float)Math.Round(value, 1));
            this.RaisePropertyChanged(nameof(TrimTopMax));
        }
    }

    public float TrimBottomMax => 99 - TrimTop;


    public string RedTimings
    {
        get => _redTimings;
        set => this.RaiseAndSetIfChanged(ref _redTimings, value);
    }

    public string GreenTimings
    {
        get => _greenTimings;
        set => this.RaiseAndSetIfChanged(ref _greenTimings, value);
    }

    public string BlueTimings
    {
        get => _blueTimings;
        set => this.RaiseAndSetIfChanged(ref _blueTimings, value);
    }

    public byte LowerThreshold
    {
        get => _lowerThreshold;
        set
        {
            this.RaiseAndSetIfChanged(ref _lowerThreshold, value);
            this.RaisePropertyChanged(nameof(UpperThresholdMin));
        }
    }

    public byte UpperThreshold
    {
        get => _upperThreshold;
        set
        {
            this.RaiseAndSetIfChanged(ref _upperThreshold, value);
            this.RaisePropertyChanged(nameof(LowerThresholdMax));
        }
    }

    public int LowerThresholdMax => UpperThreshold - 1;
    public int UpperThresholdMin => LowerThreshold + 1;

    public int ScaleDownFactor
    {
        get => _scaleDownFactor;
        set
        {
            this.RaiseAndSetIfChanged(ref _scaleDownFactor, value);
            this.RaisePropertyChanged(nameof(ScaledDownResolution));
        }
    }

    public string ScaledDownResolution => SelectedCharacteristic != null
        ? $"{SelectedCharacteristic?.Width / ScaleDownFactor}x{SelectedCharacteristic?.Height / ScaleDownFactor}"
        : "";

    public ChannelSeparationMode ChannelSeparationMode
    {
        get => _channelSeparationMode;
        set => this.RaiseAndSetIfChanged(ref _channelSeparationMode, value);
    }

    public LightControlViewModel LightControlViewModel { get; } = new();
    public ScanViewModel ScanViewModel { get; } = new();
    private IImage? _lastRawFrame;

    public IImage? LastRawFrame
    {
        get => _lastRawFrame;
        set => this.RaiseAndSetIfChanged(ref _lastRawFrame, value);
    }

    private MainWindowTabItem _selectedTab;

    public MainWindowTabItem SelectedTab
    {
        get => _selectedTab;
        set => this.RaiseAndSetIfChanged(ref _selectedTab, value);
    }
}
