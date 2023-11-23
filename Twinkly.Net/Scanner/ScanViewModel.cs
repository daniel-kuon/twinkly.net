using System.Collections.ObjectModel;
using Avalonia.Media;
using ReactiveUI;

namespace Scanner;

public class ScanViewModel : ReactiveObject
{
    private ImageMergeMode _imageMergeMode;
    private int _threshold;
    private ScanRun? _selectedScanRun;
    private int _scanProgress;
    private bool _isScanning;
    private IImage? _lastMergedImage;

    public ImageMergeMode ImageMergeMode
    {
        get => _imageMergeMode;
        set => this.RaiseAndSetIfChanged(ref _imageMergeMode, value);
    }

    public int Threshold
    {
        get => _threshold;
        set => this.RaiseAndSetIfChanged(ref _threshold, value);
    }

    public ScanRun? SelectedScanRun
    {
        get => _selectedScanRun;
        set => this.RaiseAndSetIfChanged(ref _selectedScanRun, value);
    }

    public int ScanProgress
    {
        get => _scanProgress;
        set => this.RaiseAndSetIfChanged(ref _scanProgress, value);
    }

    public bool IsScanning
    {
        get => _isScanning;
        set => this.RaiseAndSetIfChanged(ref _isScanning, value);
    }

    public IImage? LastMergedImage
    {
        get => _lastMergedImage;
        set => this.RaiseAndSetIfChanged(ref _lastMergedImage, value);
    }

    public ObservableCollection<ScanRun> ScanRuns { get; } = new();

}
