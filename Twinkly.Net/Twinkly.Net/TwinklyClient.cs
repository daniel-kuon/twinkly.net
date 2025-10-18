using System.Buffers.Text;
using System.Data;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Twinkly.Net.DTOs.Enums;
using Twinkly.Net.DTOs.Requests;
using Twinkly.Net.DTOs.Responses;

namespace Twinkly.Net;

public partial class TwinklyClient
{
    public LedProfile LedProfile { get; private set; }
    public LedByteMode LedByteMode { get; private set; }
    public byte LedByteCount { get; private set; }
    public int LedsCount { get; private set; }
    public int MinTimeBetweenFramesMs { get; set; }
    public UdpProtocolVersion UdpProtocolVersion { get; private set; }
    private DateTime _lastFrameSent = DateTime.MinValue;


    public IDeviceDetailsResponse DeviceDetails
    {
        get =>
            field ??
            throw new
                InvalidOperationException("DeviceDetails not loaded. This indicates an issue during the client creation");
        private set
        {
            field = value;
            LedProfile = value.LedProfile;
            LedByteCount = value.LedProfile == LedProfile.Rgbw ? (byte)4 : (byte)3;
            LedByteMode = value.LedProfile == LedProfile.Rgbw ? LedByteMode.Rgbw : LedByteMode.RgbAww;
            LedsCount = value.NumberOfLed;
            UdpProtocolVersion = DetermineUdpProtocolVersion(value);
        }
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
                                                                      {
                                                                          PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                                                                          Converters =
                                                                          {
                                                                              new EnumStringConverterFactory()
                                                                          }
                                                                      };

    private string? _authenticationToken;
    private OperationMode? _ledMode;
    private Request<ICodeResponse>? _lastColorRequest;
    private Request<ICodeResponse>? _lastBrightnessRequest;
    private readonly IPAddress _ipAddress;
    private readonly ILogger<TwinklyClient> _logger;
    private readonly HttpClient _httpClient;

    private TwinklyClient(IPAddress ipAddress, ILogger<TwinklyClient> logger, HttpClient httpClient)
    {
        _ipAddress = ipAddress;
        _logger = logger;
        _httpClient = httpClient;
    }

    public static async Task<TwinklyClient> Create(IPAddress ipAddress, ILogger<TwinklyClient> logger, HttpClient httpClient)
    {
        var client = new TwinklyClient(ipAddress, logger, httpClient);
        await client.Authenticate();
        client.DeviceDetails = await client.ExecuteRequest(new DeviceDetailsRequest());
        logger.LogInformation("Connected to device {DeviceName} with {LedsCount} LEDs, profile {LedProfile}, and UDP protocol version {UdpProtocolVersion}", client.DeviceDetails.DeviceName, client.DeviceDetails.NumberOfLed, client.DeviceDetails.LedProfile, client.UdpProtocolVersion);
        return client;
    }

    private static UdpProtocolVersion DetermineUdpProtocolVersion(IDeviceDetailsResponse deviceDetails)
    {
        // Check if it's a DeviceDetailsD which has ProductVersion
        if (deviceDetails is DeviceDetailsD detailsD)
        {
            // Parse the firmware version (format: e.g., "2.4.14", "2.4.6", "1.0.0")
            var productVersion = detailsD.ProductVersion;
            if (Version.TryParse(productVersion, out var version))
            {
                // Generation II devices from firmware 2.4.14 use version 3
                if (version >= new Version(2, 4, 14))
                {
                    return UdpProtocolVersion.Version3;
                }
                // Generation II devices up to firmware 2.4.6 use version 2
                else if (version >= new Version(2, 0, 0))
                {
                    return UdpProtocolVersion.Version2;
                }
                // Generation I devices use version 1
                else
                {
                    return UdpProtocolVersion.Version1;
                }
            }
        }

        // For other device detail types (F, G) or if version parsing fails,
        // default to version 3 as it's the most recent and widely supported
        return UdpProtocolVersion.Version3;
    }

    private async Task Authenticate()
    {
        _logger.LogInformation("Connecting to {IpAddress}", _ipAddress);
        //generate random 32 char challenge
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var data = RandomNumberGenerator.GetBytes(32).Select(b => (byte)alphabet[b % alphabet.Length]).ToArray();
        var base64String = Convert.ToBase64String(data);
        var loginResponse = await ExecuteRequest(new LoginRequest(base64String));
        _authenticationToken = loginResponse.AuthenticationToken;

        _logger.LogInformation("Verifying challenge response");
        await ExecuteRequest(new VerifyRequest(loginResponse.ChallengeResponse));
    }

    public async Task SetLedMode(OperationMode mode)
    {
        if (_ledMode == mode)
        {
            return;
        }

        _lastBrightnessRequest = null;
        _lastColorRequest = null;

        await ExecuteRequest(new SetLedModeRequest(mode));
        _ledMode = mode;
    }

    private async Task SendColorRequest(Request<ICodeResponse> request)
    {
        await SetLedMode(OperationMode.Color);
        if (_lastColorRequest?.Equals(request) == true)
        {
            return;
        }

        await ExecuteRequest(request);

        _lastColorRequest = request;
    }

    private async Task SendBrightnessRequest(SetBrightnessRequest request)
    {
        if (_lastBrightnessRequest?.Equals(request) == true)
        {
            return;
        }

        await ExecuteRequest(request);

        _lastBrightnessRequest = request;
    }

    private async Task<TResponse> ExecuteRequest<TResponse>(Request<TResponse> request) where TResponse : ICodeResponse
    {
        var requestMessage = new HttpRequestMessage(request.Method, $"http://{_ipAddress}{request.Path}")
        {
            Content = JsonContent.Create(request, request.GetType(), null,  SerializerOptions)
        };


        return await ExecuteRequest<TResponse>(requestMessage, request.RequiresAuthentication);
    }

    private async Task<TResponse> ExecuteRequest<TResponse>(HttpRequestMessage requestMessage,
        bool requiresAuthentication)
        where TResponse : ICodeResponse
    {
        if (requiresAuthentication)
        {
            if (_authenticationToken is null)
            {
                await Authenticate();
            }

            requestMessage.Headers.Add("X-Auth-Token", _authenticationToken);
        }

        var response = await _httpClient.SendAsync(requestMessage);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await Authenticate();
            requestMessage = new HttpRequestMessage(requestMessage.Method, requestMessage.RequestUri)
            {
                Content = requestMessage.Content
            };
            requestMessage.Headers.Remove("X-Auth-Token");
            requestMessage.Headers.Add("X-Auth-Token", _authenticationToken);
            response = await _httpClient.SendAsync(requestMessage);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Request failed with status code {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var responseDto = JsonSerializer.Deserialize<TResponse>(responseJson, SerializerOptions);
        if (responseDto is null)
        {
            throw new NoNullAllowedException("Response DTO is null");
        }

        if (!responseDto.IsSuccess)
        {
            throw new Exception("Response DTO is not successful");
        }

        return responseDto;
    }

    /// <summary>
    /// This method sends a frame to the Twinkly device in real-time mode. It does so using the HTTP protocol.
    /// This Method is less efficient than using UDP and only allows for lower frame rates.
    /// </summary>
    /// <param name="bytes">A jagged array where each element represents the color data for a single LED. Each LED's data should be in the format defined by the device's LED profile (4 bytes per Led for RGBW and 3 bytes per Led for RGB or AWW).</param>
    /// <returns></returns>
    public async Task<ICodeResponse> SendFrame(byte[][] bytes)
    {
        ValidateAllLedsByteArray(bytes);

        await SetLedMode(OperationMode.Rt);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"http://{_ipAddress}/xled/v1/led/rt/frame")
        {
            Content = new ByteArrayContent(bytes.SelectMany(x => x).ToArray())
        };

        return await ExecuteRequest<ICodeResponse>(requestMessage, true);
    }

    /// <summary>
    /// This method sends a frame to the Twinkly device in real-time mode using UDP protocol.
    /// This method is more efficient than using HTTP and allows for higher frame rates.
    /// The appropriate UDP protocol version (1, 2, or 3) is automatically selected based on the device.
    /// </summary>
    /// <param name="bytes">A jagged array where each element represents the color data for a single LED. Each LED's data should be in the format defined by the device's LED profile (4 bytes per Led for RGBW and 3 bytes per Led for RGB or AWW).</param>
    /// <remarks>This API can send data at a higher rate than the device can process it which will lead to flickering or dropped frames. To make sure that this is no issue set the the <see cref="MinTimeBetweenFramesMs"/> property accordingly. To find this time for your device switch between full on and off leds and check at which framerate issues occur</remarks>
    public async Task SendUdpFrame(byte[][] bytes)
    {
        ValidateAllLedsByteArray(bytes);

        await SetLedMode(OperationMode.Rt);

        byte[] tokenBytes = new byte[8];
        Base64.DecodeFromUtf8(Encoding.UTF8.GetBytes(_authenticationToken!), tokenBytes, out _, out int _);

        var rawBytes = bytes.SelectMany(x => x).ToArray();

        using var udpClient = new UdpClient();
        udpClient.Connect(_ipAddress.ToString(), 7777);

        var msToWait = MinTimeBetweenFramesMs - (DateTime.UtcNow - _lastFrameSent).Milliseconds;
        if (msToWait > 0)
        {
            await Task.Delay(msToWait);
        }

        switch (UdpProtocolVersion)
        {
            case UdpProtocolVersion.Version1:
                await SendUdpFrameVersion1(udpClient, tokenBytes, rawBytes);
                break;
            case UdpProtocolVersion.Version2:
                await SendUdpFrameVersion2(udpClient, tokenBytes, rawBytes);
                break;
            case UdpProtocolVersion.Version3:
                await SendUdpFrameVersion3(udpClient, tokenBytes, rawBytes);
                break;
            default:
                throw new NotSupportedException($"UDP protocol version {UdpProtocolVersion} is not supported");
        }

        _lastFrameSent = DateTime.UtcNow;
    }

    /// <summary>
    /// Sends a frame using UDP protocol version 1 (generation I devices).
    /// Header: 1 byte version (0x01) + 8 bytes token + 1 byte LED count.
    /// Body: Frame format, no fragmentation.
    /// </summary>
    private async Task SendUdpFrameVersion1(UdpClient udpClient, byte[] tokenBytes, byte[] frameData)
    {
        // Version 1 header: version byte + 8 token bytes + LED count byte
        byte[] udpHeader = [0x01, ..tokenBytes, (byte)LedsCount];
        byte[] udpPacket = [..udpHeader, ..frameData];
        
        await udpClient.SendAsync(udpPacket, udpPacket.Length);
    }

    /// <summary>
    /// Sends a frame using UDP protocol version 2 (generation II devices, firmware ≤ 2.4.6).
    /// Header: 1 byte version (0x02) + 8 bytes token + 1 byte 0x00.
    /// Body: Movie format, no fragmentation.
    /// </summary>
    private async Task SendUdpFrameVersion2(UdpClient udpClient, byte[] tokenBytes, byte[] frameData)
    {
        // Version 2 header: version byte + 8 token bytes + 0x00 byte
        byte[] udpHeader = [0x02, ..tokenBytes, 0x00];
        byte[] udpPacket = [..udpHeader, ..frameData];
        
        await udpClient.SendAsync(udpPacket, udpPacket.Length);
    }

    /// <summary>
    /// Sends a frame using UDP protocol version 3 (generation II devices, firmware ≥ 2.4.14).
    /// Header: 1 byte version (0x03) + 8 bytes token + 2 bytes 0x00 + 1 byte fragment number.
    /// Body: Frames fragmented into UDP datagrams up to 900 bytes.
    /// </summary>
    private async Task SendUdpFrameVersion3(UdpClient udpClient, byte[] tokenBytes, byte[] frameData)
    {
        // Version 3 uses fragmentation for frames larger than 900 bytes
        const int udpPayloadLimit = 900;
        byte fragmentNumber = 0;
        var bytesSent = 0;

        while (bytesSent < frameData.Length)
        {
            // Version 3 header: version byte + 8 token bytes + 2 unknown bytes (0x00) + fragment number
            byte[] udpHeader = [0x03, ..tokenBytes, 0x00, 0x00, fragmentNumber];
            byte[] fragment = frameData.Skip(bytesSent).Take(udpPayloadLimit).ToArray();
            byte[] udpPacket = [..udpHeader, ..fragment];
            
            await udpClient.SendAsync(udpPacket, udpPacket.Length);
            
            fragmentNumber++;
            bytesSent += udpPayloadLimit;
        }
    }

    private void ValidateAllLedsByteArray(byte[][] bytes)
    {
        if (bytes.Length != LedsCount)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length,
                                                  $"Expected {nameof(bytes)} to have {LedsCount} elements");
        }

        if (bytes.Any(x => x.Length != LedByteCount))
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length,
                                                  $"Expected every element of {nameof(bytes)} to have {LedByteCount} bytes");
        }
    }
}

public enum LedByteMode
{
    RgbAww = 3,
    Rgbw = 4
}
