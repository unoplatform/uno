#nullable enable

using System;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the NavigationStarting event.
/// </summary>
public partial class CoreWebView2NavigationStartingEventArgs : EventArgs
{
	public CoreWebView2NavigationStartingEventArgs(ulong navigationId, string? uri) =>
		(NavigationId, Uri) = (navigationId, uri);

	/// <summary>
	/// Gets the ID of the navigation.
	/// </summary>
	public ulong NavigationId { get; }

	/// <summary>
	/// Gets the URI of the requested navigation.
	/// </summary>
	public string? Uri { get; }

	/// <summary>
	/// Determines whether to cancel the navigation.
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// true when the navigation is redirected.
	/// </summary>
	public bool IsRedirected { get; }

	/// <summary>
	/// true when the new window request was initiated through a user gesture.
	/// </summary>
	public bool IsUserInitiated { get; }
}
