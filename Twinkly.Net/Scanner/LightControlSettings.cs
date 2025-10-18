namespace Scanner;

public class LightControlSettings
{
    public byte Red { get; set; }
    public byte Blue { get; set; }
    public byte Green { get; set; }
    public byte White { get; set; }
    public byte Brightness { get; set; }
    public BaseColor BaseColor { get; set; }
    public int ColorChangeSpeed { get; set; }
    public LightControlMode Mode { get; set; }
    public string IpAddress { get; set; } = "";


    public static LightControlSettings FromViewModel(LightControlViewModel viewModel)
    {
        var settings = new LightControlSettings();
        Settings.CopySameNameProperties(viewModel, settings);
        return settings;
    }

    public void CopyToViewModel(LightControlViewModel viewModel)
    {
        Settings.CopySameNameProperties(this, viewModel);
    }
}
