using System.Text.Json.Serialization;

namespace Twinkly.Net.DTOs.Enums;

[EnumCase(Case.Lower)]
public enum OperationMode
{
    /// <summary>
    /// Lights are turned off
    /// </summary>
    Off,

    /// <summary>
    /// Lights show a static color
    /// </summary>
    Color,

    /// <summary>
    /// Demo mode, cycles through pre-defined effects
    /// </summary>
    Demo,

    /// <summary>
    /// Plays a predefined effect
    /// </summary>
    Effect,

    /// <summary>
    /// Plays an uploaded movie
    /// </summary>
    Movie,

    /// <summary>
    /// Cycles through playlist of uploaded movies
    /// </summary>
    Playlist,

    /// <summary>
    /// Receive effect in real time
    /// </summary>
    Rt
}
