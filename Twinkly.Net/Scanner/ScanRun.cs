using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using ReactiveUI;

namespace Scanner;

public class ScanRun(string settingsString, DateTime creationDateTime, List<LedScanResult> leds, List<Bitmap> images)
    : ReactiveObject
{
    public string SettingsString { get; } = settingsString;
    public DateTime CreationDateTime { get; } = creationDateTime;
    public List<LedScanResult> Leds { get; } = leds;
    public List<Bitmap> Images { get; } = images;

    private LedScanResult? _selectedLed;
    public LedScanResult? SelectedLed
    {
        get => _selectedLed;
        set => this.RaiseAndSetIfChanged(ref _selectedLed, value);
    }

    public override string ToString()
    {
        return $"{CreationDateTime:HH:mm:ss} - {SettingsString}";
    }
}