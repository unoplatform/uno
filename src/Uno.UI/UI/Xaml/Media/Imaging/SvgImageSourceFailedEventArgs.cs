namespace Windows.UI.Xaml.Media.Imaging;

/// <summary>
/// Provides event data for the SvgImageSource.OpenFailed event.
/// </summary>
public partial class SvgImageSourceFailedEventArgs
{
	internal SvgImageSourceFailedEventArgs(SvgImageSourceLoadStatus status)
	{
		Status = status;
	}

	/// <summary>
	/// Gets a value that indicates the reason for the SVG loading failure.
	/// </summary>
	public SvgImageSourceLoadStatus Status { get; }
}
