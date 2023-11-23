namespace Scanner;

public class LightControlSettings
{
    public int Red { get; set; }
    public int Blue { get; set; }
    public int Green { get; set; }
    public byte Brightness { get; set; }
    public BaseColor BaseColor { get; set; }
    public int ColorChangeSpeed { get; set; }
    public LightControlMode Mode { get; set; }
    public string IpAddress { get; set; } = "";


    public static LightControlSettings FromViewModel(LightControlViewModel viewModel)
    {
        return new LightControlSettings
        {
            Red = viewModel.Red,
            Blue = viewModel.Blue,
            Green = viewModel.Green,
            Brightness = viewModel.Brightness,
            BaseColor = viewModel.BaseColor,
            ColorChangeSpeed = viewModel.ColorChangeSpeed,
            Mode = viewModel.Mode,
            IpAddress = viewModel.IpAddress
        };
    }

    public void CopyToViewModel(LightControlViewModel viewModel)
    {
        viewModel.Red = Red;
        viewModel.Blue = Blue;
        viewModel.Green = Green;
        viewModel.Brightness = Brightness;
        viewModel.BaseColor = BaseColor;
        viewModel.ColorChangeSpeed = ColorChangeSpeed;
        viewModel.Mode = Mode;
        viewModel.IpAddress = IpAddress;
    }
}
