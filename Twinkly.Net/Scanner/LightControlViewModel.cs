using System;
using System.Reactive;
using System.Text.Json.Serialization;
using Avalonia.Threading;
using ReactiveUI;
using Twinkly.Net.DTOs.Requests;

namespace Scanner;

public class LightControlViewModel : ReactiveObject
{
    private int _red;

    public int Red
    {
        get => _red;
        set => this.RaiseAndSetIfChanged(ref _red, value);
    }

    private int _blue;

    public int Blue
    {
        get => _blue;
        set => this.RaiseAndSetIfChanged(ref _blue, value);
    }

    private int _green;

    public int Green
    {
        get => _green;
        set => this.RaiseAndSetIfChanged(ref _green, value);
    }

    private byte _brightness;

    public byte Brightness
    {
        get => _brightness;
        set => this.RaiseAndSetIfChanged(ref _brightness, value);
    }

    private BaseColor _baseColor;

    public BaseColor BaseColor
    {
        get => _baseColor;
        set => this.RaiseAndSetIfChanged(ref _baseColor, value);
    }

    private int _colorChangeSpeed;

    public int ColorChangeSpeed
    {
        get => _colorChangeSpeed;
        set => this.RaiseAndSetIfChanged(ref _colorChangeSpeed, Math.Max(1,value));
    }

    private LightControlMode _mode;

    public LightControlMode Mode
    {
        get => _mode;
        set
        {
            this.RaiseAndSetIfChanged(ref _mode, value);
            this.RaisePropertyChanged(nameof(ShowColorPicker));
            this.RaisePropertyChanged(nameof(ShowBaseColorPicker));
            this.RaisePropertyChanged(nameof(ShowBrightness));
            this.RaisePropertyChanged(nameof(ShowSpeedSlider));
        }
    }

    private string _ipAddress = "";

    public string IpAddress
    {
        get => _ipAddress;
        set => this.RaiseAndSetIfChanged(ref _ipAddress, value);
    }

    private bool _isConnected;

    public bool IsConnected
    {
        get => _isConnected;
        set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }

    [JsonIgnore] public bool ShowColorPicker => Mode == LightControlMode.CustomColor;

    [JsonIgnore] public bool ShowBaseColorPicker => Mode == LightControlMode.BaseColor;

    [JsonIgnore]
    public bool ShowBrightness =>
        Mode is LightControlMode.BaseColor or LightControlMode.ColorChange or LightControlMode.LedScanPattern;

    [JsonIgnore] public bool ShowSpeedSlider => Mode is LightControlMode.ColorChange or LightControlMode.LedScanPattern;

}
