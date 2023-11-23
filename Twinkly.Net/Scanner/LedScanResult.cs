using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Scanner;

public class LedScanResult(int index, byte[][,] ledImageByteArrays, byte[,] mergedByteArray)
{
    private IImage[]? _ledImages;
    private IImage? _mergedImage;
    public int Index { get; } = index;
    public IImage MergedImage => _mergedImage ??= ImageByteArrayConverter.ConvertToImage(mergedByteArray);

    public IImage[] LedImages => _ledImages ??= ConvertByteArraysToBitmaps();

    private IImage[] ConvertByteArraysToBitmaps()
    {
        return ledImageByteArrays.Select(ImageByteArrayConverter.ConvertToImage).ToArray();
    }
}
