using System;
using System.Reactive;
using System.Text.Json.Serialization;
using Avalonia.Threading;
using ReactiveUI;
using Twinkly.Net.DTOs.Requests;

namespace Scanner;

public class LightControlViewModel : ReactiveObject
{
    public byte Red
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public byte Blue
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public byte Green
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public byte White
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public byte Brightness
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public int BaseColorIndex
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public BaseColor BaseColor
    {
        get => (BaseColor)BaseColorIndex;
        set => BaseColorIndex = (int)value;
    }

    public int ColorChangeSpeed
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, Math.Max(1, value));
    }

    public int ModeIndex
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(ShowColorPicker));
            this.RaisePropertyChanged(nameof(ShowBaseColorPicker));
            this.RaisePropertyChanged(nameof(ShowBrightness));
            this.RaisePropertyChanged(nameof(ShowSpeedSlider));
            this.RaisePropertyChanged(nameof(Mode));
        }
    }

    public LightControlMode Mode
    {
        get => (LightControlMode)ModeIndex;
        set => ModeIndex = (int)value;
    }

    public string IpAddress
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    public bool IsConnected
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool RgbwMode
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool Activated
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(Mode));
        }
    }

    public string Timings
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    [JsonIgnore] public bool ShowColorPicker => Mode is LightControlMode.CustomColor or LightControlMode.LedScanPattern;

    [JsonIgnore] public bool ShowBaseColorPicker => Mode == LightControlMode.BaseColor;

    [JsonIgnore]
    public bool ShowBrightness =>
        Mode is LightControlMode.BaseColor or LightControlMode.ColorChange;

    [JsonIgnore] public bool ShowSpeedSlider => Mode is LightControlMode.ColorChange or LightControlMode.LedScanPattern;
}
