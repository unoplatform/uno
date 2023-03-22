#nullable enable

using System;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the CoreWebView2.ContentLoading event.
/// </summary>
public partial class CoreWebView2ContentLoadingEventArgs
{
	internal CoreWebView2ContentLoadingEventArgs(ulong navigationId, Uri? uri, bool isErrorPage) =>
		(NavigationId, Uri, IsErrorPage) = (navigationId, uri, isErrorPage);

	/// <summary>
	/// Gets the ID of the navigation.
	/// </summary>
	public ulong NavigationId { get; }

	/// <summary>
	/// True if the loaded content is an error page.
	/// </summary>
	public bool IsErrorPage { get; }

	internal Uri? Uri { get; }
}
