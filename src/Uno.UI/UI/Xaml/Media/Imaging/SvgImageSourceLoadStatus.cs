namespace Windows.UI.Xaml.Media.Imaging;

/// <summary>
/// Defines constants that specify the result of loading an SvgImageSource.
/// </summary>
public enum SvgImageSourceLoadStatus
{
	/// <summary>
	/// The SVG loaded.
	/// </summary>
	Success,

	/// <summary>
	/// The SVG did not load due to a network error.
	/// </summary>
	NetworkError,

	/// <summary>
	/// The SVG did not load because the SVG format is invalid.
	/// </summary>
	InvalidFormat,

	/// <summary>
	/// The SVG did not load for some other reason.
	/// </summary>
	Other,
}
