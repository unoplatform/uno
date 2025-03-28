#nullable enable

using System;
using Windows.Web;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides data for the WebView.NavigationCompleted and FrameNavigationCompleted events.
/// </summary>
public sealed partial class WebViewNavigationCompletedEventArgs
{
	internal WebViewNavigationCompletedEventArgs(
		bool isSuccess,
		Uri? uri,
		WebErrorStatus webErrorStatus) =>
		(IsSuccess, Uri, WebErrorStatus) = (isSuccess, uri, webErrorStatus);

	/// <summary>
	/// Gets a value that indicates whether the navigation completed successfully.
	/// </summary>
	public bool IsSuccess { get; }

	/// <summary>
	/// Gets the Uniform Resource Identifier (URI) of the WebView content.
	/// </summary>
	public Uri? Uri { get; }

	/// <summary>
	/// If the navigation was unsuccessful, gets a value that indicates why.
	/// </summary>
	public WebErrorStatus WebErrorStatus { get; }
}
