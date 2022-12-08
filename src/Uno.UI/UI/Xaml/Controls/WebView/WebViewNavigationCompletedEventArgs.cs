using System;
using Windows.Web;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides data for the WebView.NavigationCompleted and FrameNavigationCompleted events.
/// </summary>
public sealed partial class WebViewNavigationCompletedEventArgs
{
	/// <summary>
	/// Gets a value that indicates whether the navigation completed successfully.
	/// </summary>
	public bool IsSuccess { get; internal set; }

	/// <summary>
	/// Gets the Uniform Resource Identifier (URI) of the WebView content.
	/// </summary>
	public Uri Uri { get; internal set; }

	/// <summary>
	/// If the navigation was unsuccessful, gets a value that indicates why.
	/// </summary>
	public WebErrorStatus WebErrorStatus { get; internal set; }
}
