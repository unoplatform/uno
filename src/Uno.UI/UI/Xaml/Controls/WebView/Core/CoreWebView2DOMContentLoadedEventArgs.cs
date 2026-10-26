namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2DOMContentLoadedEventArgs
{
	internal CoreWebView2DOMContentLoadedEventArgs(ulong navigationId)
	{
		NavigationId = navigationId;
	}

	public ulong NavigationId { get; }
}