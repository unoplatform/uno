namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2ContentLoadingEventArgs
{
	internal CoreWebView2ContentLoadingEventArgs(bool isErrorPage, ulong navigationId)
	{
		IsErrorPage = isErrorPage;
		NavigationId = navigationId;
	}

	public bool IsErrorPage { get; }

	public ulong NavigationId { get; }
}