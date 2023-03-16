namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the CoreWebView2.DOMContentLoaded event.
/// </summary>
public partial class CoreWebView2DOMContentLoadedEventArgs
{
	internal CoreWebView2DOMContentLoadedEventArgs(ulong navigationId) => NavigationId = navigationId;

	/// <summary>
	/// Gets the ID of the navigation.
	/// </summary>
	public ulong NavigationId { get; }
}
