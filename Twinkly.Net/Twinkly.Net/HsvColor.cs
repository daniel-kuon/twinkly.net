namespace Twinkly.Net;

public class HsvColor
{
    public HsvColor(int hue, int saturation, int value)
    {
        Hue = hue;
        Saturation = saturation;
        Value = value;
    }

    public int Hue { get; }
    public int Saturation { get; }
    public int Value { get; }
}