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

/// <summary>
/// Client for interacting with Twinkly LED devices via their REST API.
/// </summary>
public partial class TwinklyClient
{
    /// <summary>
    /// Gets the LED profile of the device (RGB, RGBW, or AWW).
    /// </summary>
    public LedProfile LedProfile { get; private set; }
    
    /// <summary>
    /// Gets the LED byte mode (3 bytes for RGB/AWW, 4 bytes for RGBW).
    /// </summary>
    public LedByteMode LedByteMode { get; private set; }
    
    /// <summary>
    /// Gets the number of bytes per LED.
    /// </summary>
    public byte LedByteCount { get; private set; }
    
    /// <summary>
    /// Gets the total number of LEDs in the device.
    /// </summary>
    public int LedsCount { get; private set; }
    
    /// <summary>
    /// Gets or sets the minimum time between frames in milliseconds for UDP frame sending.
    /// Set this to avoid flickering or dropped frames.
    /// </summary>
    public int MinTimeBetweenFramesMs { get; set; }
    private DateTime _lastFrameSent = DateTime.MinValue;


    /// <summary>
    /// Gets the detailed device information retrieved during client creation.
    /// </summary>
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

    /// <summary>
    /// Creates and initializes a new TwinklyClient instance.
    /// Automatically authenticates and retrieves device details.
    /// </summary>
    /// <param name="ipAddress">IP address of the Twinkly device</param>
    /// <param name="logger">Logger instance for logging operations</param>
    /// <param name="httpClient">HTTP client for making requests</param>
    /// <returns>Initialized TwinklyClient</returns>
    public static async Task<TwinklyClient> Create(IPAddress ipAddress, ILogger<TwinklyClient> logger, HttpClient httpClient)
    {
        var client = new TwinklyClient(ipAddress, logger, httpClient);
        await client.Authenticate();
        client.DeviceDetails = await client.ExecuteRequest(new DeviceDetailsRequest());
        logger.LogInformation("Connected to device {DeviceName} with {LedsCount} LEDs and profile {LedProfile}", client.DeviceDetails.DeviceName, client.DeviceDetails.NumberOfLed, client.DeviceDetails.LedProfile);
        return client;
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

    /// <summary>
    /// Sets the LED operation mode.
    /// </summary>
    /// <param name="mode">The operation mode to set (Off, Color, Demo, Effect, Movie, Playlist, Rt)</param>
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
    /// </summary>
    /// <param name="bytes">A jagged array where each element represents the color data for a single LED. Each LED's data should be in the format defined by the device's LED profile (4 bytes per Led for RGBW and 3 bytes per Led for RGB or AWW).</param>
    /// <remarks>This API can send data at a higher rate than the device can process it which will lead to flickering or dropped frames. To make sure that this is no issue set the the <see cref="MinTimeBetweenFramesMs"/> property accordingly. To find this time for your device switch between full on and off leds and check at which framerate issues occur</remarks>
    public async Task SendUdpFrame(byte[][] bytes)
    {
        ValidateAllLedsByteArray(bytes);

        await SetLedMode(OperationMode.Rt);

        byte[] tokenBytes = new byte[8];
        Base64.DecodeFromUtf8(Encoding.UTF8.GetBytes(_authenticationToken!), tokenBytes, out _, out int _);

        byte[] udpHeader = [3, ..tokenBytes, 0, 0];

        byte i = 0;

        var rawBytes = bytes.SelectMany(x => x).ToArray();

        using var udpClient = new UdpClient();
        udpClient.Connect(_ipAddress.ToString(), 7777);

        var bytesSend = 0;

        var msToWait = MinTimeBetweenFramesMs - (DateTime.UtcNow - _lastFrameSent).Milliseconds;
        if (msToWait > 0)
        {
            await Task.Delay(msToWait);
        }

        while (bytesSend < rawBytes.Length)
        {
            const int udpPayloadLimit = 900;
            byte[] udpPacket = [..udpHeader, i, ..rawBytes.Skip(i*udpPayloadLimit).Take(udpPayloadLimit)];
            await udpClient.SendAsync(udpPacket, udpPacket.Length);
            i++;
            bytesSend += udpPayloadLimit;
        }

        _lastFrameSent = DateTime.UtcNow;
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

/// <summary>
/// Represents the number of bytes per LED based on the device profile.
/// </summary>
public enum LedByteMode
{
    /// <summary>
    /// 3 bytes per LED (RGB or AWW profiles)
    /// </summary>
    RgbAww = 3,
    
    /// <summary>
    /// 4 bytes per LED (RGBW profile)
    /// </summary>
    Rgbw = 4
}
