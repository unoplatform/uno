namespace Windows.UI.Xaml.Media.Imaging;

/// <summary>
/// Provides event data for the DownloadProgress event.
/// </summary>
public sealed partial class DownloadProgressEventArgs
{
	/// <summary>
	/// Gets download progress as a value that is between 0 and 100.
	/// </summary>
	public int Progress { get; set; }
}
