using System.Data;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Twinkly.Net.DTOs.Enums;
using Twinkly.Net.DTOs.Requests;
using Twinkly.Net.DTOs.Responses;

namespace Twinkly.Net;

public class TwinklyClient
{
    public LedProfile LedProfile { get; private set; }
    public int LedsCount { get; private set; }


    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters =
        {
            new LowercaseStringEnumConverter<OperationMode>(),
            new UpperCaseStringEnumConverter<LedProfile>()
        }
    };

    private string? _authenticationToken;
    private OperationMode? _ledMode;
    private readonly IPAddress _ipAddress;
    private readonly ILogger<TwinklyClient> _logger;
    private readonly HttpClient _httpClient;
    private Request<CodeResponse>? _lastColorRequest;
    private Request<CodeResponse>? _lastBrightnessRequest;

    public TwinklyClient(IPAddress ipAddress, ILogger<TwinklyClient> logger, HttpClient httpClient)
    {
        _ipAddress = ipAddress;
        _logger = logger;
        _httpClient = httpClient;
    }


    public async Task Connect()
    {
        _logger.LogInformation("Connecting to {IpAddress}", _ipAddress);
        //generate random 32 char challenge
        string challenge = "";
        var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var data = RandomNumberGenerator.GetBytes(32);
        var chars = data.Select(x => alphabet[x % alphabet.Length]);
        challenge = new string(chars.ToArray());
        var bytes = Encoding.UTF8.GetBytes(challenge);
        var base64String = Convert.ToBase64String(bytes);
        var loginResponse = await ExecuteRequest(new LoginRequest(base64String));
        _authenticationToken = loginResponse.AuthenticationToken;

        _logger.LogInformation("Verifying challenge response");
        await ExecuteRequest(new VerifyRequest(loginResponse.ChallengeResponse));
        _logger.LogInformation("Challenge response verified");
        var deviceDetails = await GetDeviceDetails();
        LedsCount = deviceDetails.NumberOfLed;
        LedProfile = deviceDetails.LedProfile;
    }

    public async Task<DeviceDetailsResponse> GetDeviceDetails()
    {
        return await ExecuteRequest(new DeviceDetailsRequest());
    }

    public async Task SetDeviceName(string name)
    {
        await ExecuteRequest(new SetDeviceNameRequest(name));
    }

    public async Task<string> GetDeviceName()
    {
        var response = await ExecuteRequest(new GetDeviceNameRequest());
        return response.DeviceName;
    }

    public async Task SetLedMode(OperationMode mode)
    {
        if (_ledMode == mode)
        {
            return;
        }

        await ExecuteRequest(new SetLedModeRequest(mode));
        _ledMode = mode;
    }

    public async Task<OperationMode> GetLedMode()
    {
        var response = await ExecuteRequest(new GetLedModeRequest());
        return response.Mode;
    }

    public async Task SetHsvColor(int hue, int saturation, int value)
    {
        await SetLedMode(OperationMode.Color);
        var request = new SetHsvColorRequest(hue, saturation, value);

        if (_lastColorRequest is SetHsvColorRequest lastColorRequest &&
            lastColorRequest.Hue == hue &&
            lastColorRequest.Saturation == saturation &&
            lastColorRequest.Value == value)
        {
            return;
        }

        _lastColorRequest = request;
        await ExecuteRequest(request);
    }

    public async Task SetRgbColor(int red, int green, int blue)
    {
        await SetLedMode(OperationMode.Color);
        var request = new SetRgbColorRequest(red, green, blue);
        if (_lastColorRequest is SetRgbColorRequest lastColorRequest &&
            lastColorRequest.Red == red &&
            lastColorRequest.Green == green &&
            lastColorRequest.Blue == blue)
        {
            return;
        }

        _lastColorRequest = request;
        await ExecuteRequest(request);
    }

    public async Task<ColorResponse> GetColor()
    {
        return await ExecuteRequest(new GetColorRequest());
    }

    public async Task SetBrightness(int brightness)
    {
        await SendBrightnessRequest(new SetBrightnessRequest(brightness));
    }

    private async Task SendBrightnessRequest(SetBrightnessRequest request)
    {
        if (_lastBrightnessRequest is SetBrightnessRequest lastBrightnessRequest &&
            lastBrightnessRequest.Brightness == request.Brightness && lastBrightnessRequest.Mode == request.Mode &&
            lastBrightnessRequest.Type == request.Type)
        {
            return;
        }

        _lastBrightnessRequest = request;

        await ExecuteRequest(request);
    }

    public async Task SetBrightness(BrightnessMode mode)
    {
        await SendBrightnessRequest(new SetBrightnessRequest(mode));
    }

    public async Task SetBrightness(int brightness, BrightnessMode mode)
    {
        await SendBrightnessRequest(new SetBrightnessRequest(brightness, mode));
    }

    public async Task SetBrightness(int brightness, BrightnessMode mode, BrightnessType type)
    {
        await SendBrightnessRequest(new SetBrightnessRequest(brightness, mode, type));
    }

    public async Task<BrightnessResponse> GetBrightness()
    {
        return await ExecuteRequest(new GetBrightnessRequest());
    }


    private async Task<TResponse> ExecuteRequest<TResponse>(Request<TResponse> request) where TResponse : CodeResponse
    {
        var requestJson = JsonSerializer.Serialize(request, request.GetType(), _serializerOptions);
        var requestMessage = new HttpRequestMessage(request.Method, $"http://{_ipAddress}{request.Path}")
        {
            Content = new StringContent(requestJson)
        };


        return await ExecuteRequest<TResponse>(requestMessage, request.RequiresAuthentication);
    }

    private async Task<TResponse> ExecuteRequest<TResponse>(HttpRequestMessage requestMessage,
        bool requiresAuthentication)
        where TResponse : CodeResponse
    {
        if (requiresAuthentication)
        {
            if (_authenticationToken is null)
            {
                throw new NoNullAllowedException("Authentication token is null");
            }

            requestMessage.Headers.Add("X-Auth-Token", _authenticationToken);
        }

        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Request failed with status code {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var responseDto = JsonSerializer.Deserialize<TResponse>(responseJson, _serializerOptions);
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

    public async Task<CodeResponse> SendFrame(byte[][] bytes)
    {
        if (bytes.Length != LedsCount)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length,
                $"Expected {nameof(bytes)} to have {LedsCount} elements");
        }

        var bytesPerLed = LedProfile == LedProfile.Rgb ? 3 : 4;

        if (bytes.Any(x => x.Length != bytesPerLed))
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), bytes.Length,
                $"Expected every element of {nameof(bytes)} to have {bytesPerLed} bytes");
        }

        await SetLedMode(OperationMode.Rt);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"http://{_ipAddress}/xled/v1/led/rt/frame")
        {
            Content = new ByteArrayContent(bytes.SelectMany(x => x).ToArray())
        };

        return await ExecuteRequest<CodeResponse>(requestMessage, true);
    }
}
