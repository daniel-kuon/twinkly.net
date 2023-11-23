namespace Scanner;

public class ScanSettings
{
    public ImageMergeMode ImageMergeMode { get; set; }
    public int Threshold { get; set; }

    public static ScanSettings FromViewModel(ScanViewModel viewModel)
    {
        return new ScanSettings
        {
            ImageMergeMode = viewModel.ImageMergeMode,
            Threshold = viewModel.Threshold
        };
    }

    public void CopyToViewModel(ScanViewModel viewModel)
    {
        viewModel.ImageMergeMode = this.ImageMergeMode;
        viewModel.Threshold = this.Threshold;
    }
}