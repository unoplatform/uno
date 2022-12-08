using System;
using Windows.Web;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides data for the WebView.NavigationFailed event.
/// </summary>
public partial class WebViewNavigationFailedEventArgs
{
	/// <summary>
	/// Gets the URI that the WebView attempted to navigate to.
	/// </summary>
	public Uri Uri { get; internal set; }

	/// <summary>
	/// Gets the error that occurred when navigation failed.
	/// </summary>
	public WebErrorStatus WebErrorStatus { get; internal set; }
}
