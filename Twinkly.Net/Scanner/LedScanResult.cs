using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using DynamicData;
using ReactiveUI;

namespace Scanner;

public class LedScanResult(
    int index,
    byte[][,] ledImageByteArrays,
    byte[,] mergedByteArray,
    (double Top, double Left)? center,
    byte[][][] ledScanPatternFrames): ReactiveObject
{
    private IImage[]? _ledImages;
    private IImage? _mergedImage;
    private string? _description;
    public int Index { get; } = index;

    public string Description => _description ??= GenerateDescription();

    private string GenerateDescription()
    {
        var ledCount = ledImageByteArrays.Length;
        var centerString = Center.HasValue ? $" at {Center.Value.Left}, {Center.Value.Top}" : "";
        return $"LED {Index + 1} of {ledCount}{centerString}";
    }

    public IImmutableSolidColorBrush BackgroundColor => Center.HasValue ? Brushes.DarkGreen : Brushes.DarkRed;

    public IImage MergedImage => _mergedImage ??= ImageByteArrayConverter.ConvertToImage(mergedByteArray);

    public IImage[] LedImages => _ledImages ??= ConvertByteArraysToBitmaps();

    public (double Top, double Left)? Center { get; } = center;

    private IImage[] ConvertByteArraysToBitmaps()
    {
        return ledImageByteArrays.Select(ImageByteArrayConverter.ConvertToImage).ToArray();
    }

    private IImage? _selectedImage;

    public IImage? SelectedImage
    {
        get => _selectedImage;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedImage, value);
            this.RaisePropertyChanged(nameof(SelectedLedScanPatternFrame));
        }
    }

    public byte[][][] LedScanPatternFrames { get; } = ledScanPatternFrames;

    public byte[][]? SelectedLedScanPatternFrame =>SelectedImage == null ? null :  LedScanPatternFrames[LedImages.IndexOf(SelectedImage)];
}
