using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Scanner;

public class ImageByteArrayConverter
{

    public static IImage ConvertToImage(byte[,] channelArr)
    {
        return ConvertToImage(channelArr, channelArr, channelArr);
    }


    public static IImage ConvertToImage(byte[,] redChannelArr, byte[,] greenChannelArr, byte[,] blueChannelArr)
    {
        int height = redChannelArr.GetLength(0);
        int width = redChannelArr.GetLength(1);
        var result = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96),
            PixelFormat.Rgba8888, AlphaFormat.Unpremul);
        using var buf = result.Lock();
        Parallel.For(0, height, y =>
        {
            var rowStartAddress = buf.Address + y * buf.RowBytes;
            for (var x = 0; x < width; x++)
            {
                var color = 0xFF000000u | ((uint)blueChannelArr[y, x] << 16) | ((uint)greenChannelArr[y, x] << 8) |
                            redChannelArr[y, x];
                Marshal.WriteInt32(rowStartAddress + x * 4, (int)color);
            }
        });

        return result;
    }

}
