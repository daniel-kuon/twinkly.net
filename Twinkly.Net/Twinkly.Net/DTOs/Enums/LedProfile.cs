using System.Text.Json.Serialization;

namespace Twinkly.Net.DTOs.Enums;

[EnumCase(Case.Upper)]
public enum LedProfile
{
    Rgb,
    Rgbw,
    Aww,
}
