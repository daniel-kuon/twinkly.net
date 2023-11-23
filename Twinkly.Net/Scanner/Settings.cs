using System.IO;
using System.Linq;
using System.Text.Json;

namespace Scanner;

public class Settings
{
    public string? SelectedCamera { get; set; }
    public int? SelectedVerticalResolution { get; set; }
    public int? SelectedHorizontalResolution { get; set; }
    public bool TimeAverage { get; set; }
    public int TimeAverageSampleSize { get; set; } = 10;
    public float TrimBottom { get; set; }
    public bool TrimImage { get; set; }
    public float TrimLeft { get; set; }
    public float TrimRight { get; set; }
    public float TrimTop { get; set; }
    public byte UpperThreshold { get; set; } = 255;
    public byte LowerThreshold { get; set; }
    public int ScaleDownFactor { get; set; }
    public bool Blur { get; set; }
    public int BlurSize { get; set; } = 3;

    public ScanSettings ScanSettings { get; set; } = new();

    public LightControlSettings LightControlSettings { get; set; } = new();

    public void Save()
    {
        var settings = JsonSerializer.Serialize(this, new JsonSerializerOptions {WriteIndented = true});
        File.WriteAllText("settings.json", settings);
    }

    public static Settings Load()
    {
        if (!File.Exists("settings.json"))
        {
            return new Settings();
        }

        var settings = File.ReadAllText("settings.json");
        return JsonSerializer.Deserialize<Settings>(settings) ?? new Settings();
    }

    public static Settings FromViewModel(MainWindowViewModel viewModel)
    {
        return new Settings
        {
            SelectedCamera = viewModel.SelectedCamera?.Name,
            SelectedVerticalResolution = viewModel.SelectedCharacteristic?.Height,
            SelectedHorizontalResolution = viewModel.SelectedCharacteristic?.Width,
            TimeAverage = viewModel.TimeAverage,
            TimeAverageSampleSize = viewModel.TimeAverageSampleSize,
            TrimBottom = viewModel.TrimBottom,
            TrimImage = viewModel.TrimImage,
            TrimLeft = viewModel.TrimLeft,
            TrimRight = viewModel.TrimRight,
            TrimTop = viewModel.TrimTop,
            UpperThreshold = viewModel.UpperThreshold,
            LowerThreshold = viewModel.LowerThreshold,
            ScaleDownFactor = viewModel.ScaleDownFactor,
            Blur = viewModel.Blur,
            BlurSize = viewModel.BlurSize,
            ChannelSeparationMode = viewModel.ChannelSeparationMode,
            LightControlSettings = LightControlSettings.FromViewModel(viewModel.LightControlViewModel),
            ScanSettings = ScanSettings.FromViewModel(viewModel.ScanViewModel)
        };
    }

    public void SetToViewModel(MainWindowViewModel viewModel)
    {
        viewModel.SelectedCamera = viewModel.CameraList.FirstOrDefault(c => c.Name == SelectedCamera);
        viewModel.SelectedCharacteristic = viewModel.CharacteristicList.FirstOrDefault(c =>
            c.Height == SelectedVerticalResolution && c.Width == SelectedHorizontalResolution);
        viewModel.TimeAverage = TimeAverage;
        viewModel.TimeAverageSampleSize = TimeAverageSampleSize;
        viewModel.TrimBottom = TrimBottom;
        viewModel.TrimImage = TrimImage;
        viewModel.TrimLeft = TrimLeft;
        viewModel.TrimRight = TrimRight;
        viewModel.TrimTop = TrimTop;
        viewModel.LowerThreshold = LowerThreshold;
        viewModel.UpperThreshold = UpperThreshold;
        viewModel.ScaleDownFactor = ScaleDownFactor;
        viewModel.Blur = Blur;
        viewModel.BlurSize = BlurSize;
        viewModel.ChannelSeparationMode = ChannelSeparationMode;
        LightControlSettings.CopyToViewModel(viewModel.LightControlViewModel);
        ScanSettings.CopyToViewModel(viewModel.ScanViewModel);
    }

    public ChannelSeparationMode ChannelSeparationMode { get; set; }
}
