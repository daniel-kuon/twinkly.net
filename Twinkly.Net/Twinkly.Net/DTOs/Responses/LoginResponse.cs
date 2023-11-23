using System.Text.Json.Serialization;
using Twinkly.Net.DTOs.Enums;

namespace Twinkly.Net.DTOs.Responses;

public class LoginResponse : CodeResponse
{
    public LoginResponse(ResponseCode code, string authenticationToken, int authenticationTokenExpiresIn,
        string challengeResponse) : base(code)
    {
        AuthenticationToken = authenticationToken;
        AuthenticationTokenExpiresIn = authenticationTokenExpiresIn;
        ChallengeResponse = challengeResponse;
    }

    public string AuthenticationToken { get; }
    public int AuthenticationTokenExpiresIn { get; }

    [JsonPropertyName("challenge-response")]
    public string ChallengeResponse { get; }
}

public class DeviceDetailsD : DeviceDetailsResponse
{
    public DeviceDetailsD(ResponseCode code, string productName, string hardwareVersion, int flashSize, int ledType,
        string productCode, string deviceName, string uptime, string hwId, string mac, string uuid, int maxSupportedLed,
        int numberOfLed, LedProfile ledProfile, double frameRate, int movieCapacity, string copyright, string productVersion,
        int bytesPerLed, string ledVersion, int rssi, int baseLedsNumber) : base(code, productName,
        hardwareVersion, flashSize, ledType, productCode, deviceName, uptime, hwId, mac, uuid, maxSupportedLed, numberOfLed,
        ledProfile, frameRate, movieCapacity, copyright)
    {
        ProductVersion = productVersion;
        BytesPerLed = bytesPerLed;
        LedVersion = ledVersion;
        Rssi = rssi;
        BaseLedsNumber = baseLedsNumber;
    }

    public string ProductVersion { get; set; }
    public int BytesPerLed { get; set; }
    public string LedVersion { get; set; }
    public int Rssi { get; set; }
    public int BaseLedsNumber { get; set; }
}

public class DeviceDetailsF : DeviceDetailsResponse
{
    public DeviceDetailsF(ResponseCode code, string productName, string hardwareVersion, int flashSize, int ledType,
        string productCode, string deviceName, string uptime, string hwId, string mac, string uuid, int maxSupportedLed,
        int numberOfLed, LedProfile ledProfile, double frameRate, int movieCapacity, string copyright, string fwFamily,
        int bytesPerLed, double measuredFrameRate) : base(code, productName, hardwareVersion, flashSize,
        ledType, productCode, deviceName, uptime, hwId, mac, uuid, maxSupportedLed, numberOfLed, ledProfile, frameRate,
        movieCapacity, copyright)
    {
        FwFamily = fwFamily;
        BytesPerLed = bytesPerLed;
        MeasuredFrameRate = measuredFrameRate;
    }

    public string FwFamily { get; set; }
    public int BytesPerLed { get; set; }
    public double MeasuredFrameRate { get; set; }
}

public class DeviceDetailsG : DeviceDetailsResponse
{
    public DeviceDetailsG(ResponseCode code, string productName, string hardwareVersion, int flashSize, int ledType,
        string productCode, string deviceName, string uptime, string hwId, string mac, string uuid, int maxSupportedLed,
        int numberOfLed, LedProfile ledProfile, double frameRate, int movieCapacity, string copyright, string fwFamily,
        double measuredFrameRate, int wireType) : base(code, productName, hardwareVersion, flashSize,
        ledType, productCode, deviceName, uptime, hwId, mac, uuid, maxSupportedLed, numberOfLed, ledProfile, frameRate,
        movieCapacity, copyright)
    {
        FwFamily = fwFamily;
        MeasuredFrameRate = measuredFrameRate;
        WireType = wireType;
    }

    public string FwFamily { get; set; }
    public double MeasuredFrameRate { get; set; }
    public int WireType { get; set; }
}

public class DeviceDetailsResponse : CodeResponse
{
    public DeviceDetailsResponse(ResponseCode code, string productName, string hardwareVersion, int flashSize,
        int ledType, string productCode, string deviceName, string uptime, string hwId, string mac, string uuid,
        int maxSupportedLed, int numberOfLed, LedProfile ledProfile, double frameRate, int movieCapacity,
        string copyright) : base(code)
    {
        ProductName = productName;
        HardwareVersion = hardwareVersion;
        FlashSize = flashSize;
        LedType = ledType;
        ProductCode = productCode;
        DeviceName = deviceName;
        Uptime = uptime;
        HwId = hwId;
        Mac = mac;
        Uuid = uuid;
        MaxSupportedLed = maxSupportedLed;
        NumberOfLed = numberOfLed;
        LedProfile = ledProfile;
        FrameRate = frameRate;
        MovieCapacity = movieCapacity;
        Copyright = copyright;
    }

    public string ProductName { get; set; }
    public string HardwareVersion { get; set; }
    public int FlashSize { get; set; }
    public int LedType { get; set; }
    public string ProductCode { get; set; }
    public string DeviceName { get; set; }
    public string Uptime { get; set; }
    public string HwId { get; set; }
    public string Mac { get; set; }
    public string Uuid { get; set; }
    public int MaxSupportedLed { get; set; }
    public int NumberOfLed { get; set; }
    public LedProfile LedProfile { get; set; }
    public double FrameRate { get; set; }
    public int MovieCapacity { get; set; }
    public string Copyright { get; set; }
}

public class DeviceNameResponse :CodeResponse
{
    public DeviceNameResponse(ResponseCode code, string deviceName) : base(code)
    {
        DeviceName = deviceName;
    }

    public string DeviceName { get; set; }
}
