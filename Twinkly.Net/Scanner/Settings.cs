using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scanner;

public class Settings
{
    private static JsonSerializerOptions _jsonSerializerOptions =
        new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    public string? SelectedCameraName { get; set; }
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
    public bool SubtractBaseImage { get; set; }

    public ScanSettings ScanSettings { get; set; } = new();

    public LightControlSettings LightControlSettings { get; set; } = new();

    public void Save()
    {
        var settings = JsonSerializer.Serialize(this, _jsonSerializerOptions);
        File.WriteAllText("settings.json", settings);
    }

    public static Settings Load()
    {
        if (!File.Exists("settings.json"))
        {
            return new Settings();
        }

        var settings = File.ReadAllText("settings.json");
        return JsonSerializer.Deserialize<Settings>(settings, _jsonSerializerOptions) ?? new Settings();
    }

    public static Settings FromViewModel(MainWindowViewModel viewModel)
    {
        var settings = new Settings
        {
            SelectedCameraName = viewModel.SelectedCamera?.Name,
            SelectedVerticalResolution = viewModel.SelectedCharacteristic?.Height,
            SelectedHorizontalResolution = viewModel.SelectedCharacteristic?.Width,
            LightControlSettings = LightControlSettings.FromViewModel(viewModel.LightControlViewModel),
            ScanSettings = ScanSettings.FromViewModel(viewModel.ScanViewModel)
        };
        CopySameNameProperties(viewModel, settings);
        return settings;
    }

    public void SetToViewModel(MainWindowViewModel viewModel)
    {
        viewModel.SelectedCamera = viewModel.CameraList.FirstOrDefault(c => c.Name == SelectedCameraName);
        viewModel.SelectedCharacteristic = viewModel.CharacteristicList.FirstOrDefault(c =>
            c.Height == SelectedVerticalResolution && c.Width == SelectedHorizontalResolution);
        CopySameNameProperties(this, viewModel);
        LightControlSettings.CopyToViewModel(viewModel.LightControlViewModel);
        ScanSettings.CopyToViewModel(viewModel.ScanViewModel);
    }

    public static void CopySameNameProperties(object source, object target)
    {
        var sourceProperties = source.GetType().GetProperties();
        var targetProperties = target.GetType().GetProperties();
        foreach (var sourceProperty in sourceProperties)
        {
            var targetProperty = targetProperties.FirstOrDefault(p => p.Name == sourceProperty.Name);
            if (targetProperty == null)
            {
                continue;
            }

            targetProperty.SetValue(target, sourceProperty.GetValue(source));
        }
    }

    public ChannelSeparationMode ChannelSeparationMode { get; set; }
}
