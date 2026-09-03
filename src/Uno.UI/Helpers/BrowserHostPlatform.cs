namespace Uno.UI.Helpers;

/// <summary>
/// The mobile OS a browser is running on, as detected from the browser itself.
/// </summary>
internal enum BrowserHostPlatform
{
	/// <summary>
	/// Not running in a browser, or running in a browser that is hosted neither by iOS/iPadOS nor by Android.
	/// </summary>
	Other,

	/// <summary>
	/// iOS or iPadOS, whichever browser engine is in use (they are all WebKit on those platforms).
	/// </summary>
	iOS,

	Android,
}
