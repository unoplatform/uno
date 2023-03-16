namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the CoreWebView2.ContentLoading event.
/// </summary>
public partial class CoreWebView2ContentLoadingEventArgs
{
	internal CoreWebView2ContentLoadingEventArgs(ulong navigationId, bool isErrorPage) =>
		(NavigationId, IsErrorPage) = (navigationId, isErrorPage);

	/// <summary>
	/// Gets the ID of the navigation.
	/// </summary>
	public ulong NavigationId { get; }

	/// <summary>
	/// True if the loaded content is an error page.
	/// </summary>
	public bool IsErrorPage { get; }
}
