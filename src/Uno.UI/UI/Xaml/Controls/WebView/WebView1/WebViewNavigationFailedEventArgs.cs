using System;
using Windows.Web;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides data for the WebView.NavigationFailed event.
/// </summary>
public partial class WebViewNavigationFailedEventArgs
{
	internal WebViewNavigationFailedEventArgs(Uri uri, WebErrorStatus webErrorStatus) =>
		(Uri, WebErrorStatus) = (uri, webErrorStatus);

	/// <summary>
	/// Gets the URI that the WebView attempted to navigate to.
	/// </summary>
	public Uri Uri { get; }

	/// <summary>
	/// Gets the error that occurred when navigation failed.
	/// </summary>
	public WebErrorStatus WebErrorStatus { get; }
}
