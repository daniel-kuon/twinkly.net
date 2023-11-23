using System.Net;
using Microsoft.Extensions.Logging;
using Twinkly.Net;
using Twinkly.Net.DTOs.Enums;

// Console.WriteLine("Enter IP address of Twinkly device (default: 192.168.178.51) :");
var ipString = ""; //Console.ReadLine();
if (string.IsNullOrWhiteSpace(ipString))
{
    ipString = "192.168.178.51";
}

var ipAddress = IPAddress.Parse(ipString);

var client = new TwinklyClient(ipAddress, new LoggerFactory().CreateLogger<TwinklyClient>(), new HttpClient());

await client.Connect();
await client.SetRgbColor(255, 0, 0);

var ledIndexRepresentation = new List<int[]>();
for (var i = 0; i < client.LedsCount; i++)
{
    ledIndexRepresentation.Add(ConvertToBase3(i));
}

Byte[] red;
Byte[] green;
Byte[] blue;

if (client.LedProfile == LedProfile.Rgb)
{
    red = new byte[] { 255, 0, 0 };
    green = new byte[] { 0, 255, 0 };
    blue = new byte[] { 0, 0, 255 };
}
else
{
    red = new byte[] { 0, 255, 0, 0 };
    green = new byte[] { 0, 0, 255, 0 };
    blue = new byte[] { 0, 0, 0, 255 };
}

while (true)
{
    for (int i = 0; i < 8; i++)
    {
        var requestBody = ledIndexRepresentation.Select(l => l[i] switch
        {
            0 => red,
            1 => green,
            2 => blue,
            _ => throw new ArgumentOutOfRangeException()
        }).ToArray();

        await client.SendFrame(requestBody);
        await Task.Delay(500);
    }

    await client.SetRgbColor(0, 0, 0);
    await Task.Delay(1000);
}

while (true)
{
    var brightness = 150;
    await client.SetRgbColor(brightness, 0, 0);
    Console.ReadKey();
    await client.SetRgbColor(0, brightness, 0);
    Console.ReadKey();
    await client.SetRgbColor(0, 0, brightness);
    Console.ReadKey();
}

while (true)
{
    var brightness = 255;
    await client.SetRgbColor(brightness, 0, 0);
    await Task.Delay(1000);
    await client.SetRgbColor(0, brightness, 0);
    await Task.Delay(1000);
    await client.SetRgbColor(0, 0, brightness);
    await Task.Delay(1000);
}

int[] ConvertToBase3(int number)
{
    var digits = new List<int>();

    while (number > 0)
    {
        var remainder = number % 3;
        digits.Add(remainder);
        number /= 3;
    }

    digits.AddRange(Enumerable.Repeat(0, 8 - digits.Count));

    return digits.ToArray();
}
