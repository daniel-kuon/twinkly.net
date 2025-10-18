namespace Scanner;

public class ScanSettings
{
    public ImageMergeMode ImageMergeMode { get; set; }
    public int Threshold { get; set; }

    public static ScanSettings FromViewModel(ScanViewModel viewModel)
    {
        var settings = new ScanSettings();
        Settings.CopySameNameProperties(viewModel, settings);
        return settings;
    }

    public void CopyToViewModel(ScanViewModel viewModel)
    {
        Settings.CopySameNameProperties(this, viewModel);
    }
}
