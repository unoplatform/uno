#nullable enable

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the DOMContentLoaded event.
/// </summary>
public partial class CoreWebView2DOMContentLoadedEventArgs
{
	internal CoreWebView2DOMContentLoadedEventArgs(ulong navigationId)
	{
		NavigationId = navigationId;
	}

	/// <summary>
	/// Gets the ID of the navigation which corresponds to other navigation ID properties on other navigation events.
	/// </summary>
	public ulong NavigationId { get; }
}
