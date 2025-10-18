using System.Text.Json.Serialization;
using Twinkly.Net.DTOs.Enums;

namespace Twinkly.Net.DTOs.Responses;

/// <summary>
/// Response from login endpoint containing authentication token.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="AuthenticationToken">Access token (8 byte string base64 encoded)</param>
/// <param name="AuthenticationTokenExpiresIn">Token expiration time in seconds</param>
/// <param name="ChallengeResponse">41 byte string for verification</param>
public record LoginResponse(
    ResponseCode Code,
    string AuthenticationToken,
    int AuthenticationTokenExpiresIn,
    [property: JsonPropertyName("challenge-response")]
    string ChallengeResponse) : ICodeResponse;

/// <summary>
/// Device details response for firmware family "D".
/// </summary>
public record DeviceDetailsD(
    ResponseCode Code,
    string ProductName,
    string HardwareVersion,
    int FlashSize,
    int LedType,
    string ProductCode,
    string DeviceName,
    string Uptime,
    string HwId,
    string Mac,
    string Uuid,
    int MaxSupportedLed,
    int NumberOfLed,
    LedProfile LedProfile,
    double FrameRate,
    int MovieCapacity,
    string Copyright,
    string ProductVersion,
    int BytesPerLed,
    string LedVersion,
    int Rssi,
    char FwFamily,
    int BaseLedsNumber) : IDeviceDetailsResponse;

/// <summary>
/// Device details response for firmware family "F".
/// </summary>
public record DeviceDetailsF(
    ResponseCode Code,
    string ProductName,
    string HardwareVersion,
    int FlashSize,
    int LedType,
    string ProductCode,
    string DeviceName,
    string Uptime,
    string HwId,
    string Mac,
    string Uuid,
    int MaxSupportedLed,
    int NumberOfLed,
    LedProfile LedProfile,
    double FrameRate,
    int MovieCapacity,
    string Copyright,
    char FwFamily,
    int BytesPerLed,
    double MeasuredFrameRate) : IDeviceDetailsResponse;

/// <summary>
/// Device details response for firmware family "G".
/// </summary>
public record DeviceDetailsG(
    ResponseCode Code,
    string ProductName,
    string HardwareVersion,
    int FlashSize,
    int LedType,
    string ProductCode,
    string DeviceName,
    string Uptime,
    string HwId,
    string Mac,
    string Uuid,
    int MaxSupportedLed,
    int NumberOfLed,
    LedProfile LedProfile,
    double FrameRate,
    int MovieCapacity,
    string Copyright,
    char FwFamily,
    double MeasuredFrameRate,
    int WireType) : IDeviceDetailsResponse;

/// <summary>
/// Interface for device details response, polymorphic based on firmware family.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "fw_family")]
[JsonDerivedType(typeof(DeviceDetailsD), typeDiscriminator: "D")]
[JsonDerivedType(typeof(DeviceDetailsF), typeDiscriminator: "F")]
[JsonDerivedType(typeof(DeviceDetailsG), typeDiscriminator: "G")]
public interface IDeviceDetailsResponse : ICodeResponse
{
    public string ProductName { get; init; }
    public string HardwareVersion { get; init; }
    public int FlashSize { get; init; }
    public int LedType { get; init; }
    public string ProductCode { get; init; }
    public string DeviceName { get; init; }
    public string Uptime { get; init; }
    public string HwId { get; init; }
    public string Mac { get; init; }
    public string Uuid { get; init; }
    public int MaxSupportedLed { get; init; }
    public int NumberOfLed { get; init; }
    public LedProfile LedProfile { get; init; }
    public double FrameRate { get; init; }
    public int MovieCapacity { get; init; }
    public string Copyright { get; init; }
    new public ResponseCode Code { get; init; }
    public char FwFamily { get; init; }
}

/// <summary>
/// Response containing device name.
/// </summary>
/// <param name="Code">Response code</param>
/// <param name="DeviceName">Name of the device</param>
public record DeviceNameResponse(ResponseCode Code, string DeviceName) : ICodeResponse;
