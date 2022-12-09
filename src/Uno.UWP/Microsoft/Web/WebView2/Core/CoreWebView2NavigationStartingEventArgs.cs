using System;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the NavigationStarting event.
/// </summary>
public partial class CoreWebView2NavigationStartingEventArgs : EventArgs
{
	/// <summary>
	/// Additional allowed frame ancestors set by the host app.
	/// </summary>
	public string AdditionalAllowedFrameAncestors { get; set; }

	/// <summary>
	/// Determines whether to cancel the navigation.
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// true when the navigation is redirected.
	/// </summary>
	public bool IsRedirected { get; set; }

	/// <summary>
	/// true when the new window request was initiated through a user gesture.
	/// </summary>
	public bool IsUserInitiated { get; set; }

	/// <summary>
	/// Gets the ID of the navigation.
	/// </summary>
	public string NavigationId { get; set; }

	/// <summary>
	/// Gets the HTTP request headers for the navigation.
	/// </summary>
	public string RequestHeaders { get; set; }

	/// <summary>
	/// Gets the URI of the requested navigation.
	/// </summary>
	public Uri Uri { get; set; }
}
