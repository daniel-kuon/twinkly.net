namespace Twinkly.Net;

public class RgbColor
{
    public RgbColor(int red, int green, int blue)
    {
        Red = red;
        Green = green;
        Blue = blue;
    }

    public int Red { get; }
    public int Green { get; }
    public int Blue { get; }
}