#nullable enable

using System;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides data for the WebView.NavigationStarting and FrameNavigationStarting events.
/// </summary>
public sealed partial class WebViewNavigationStartingEventArgs
{
	internal WebViewNavigationStartingEventArgs(Uri? uri) =>
		Uri = uri;

	/// <summary>
	/// Gets the Uniform Resource Identifier (URI) of the content the WebView is loading.
	/// </summary>
	public Uri? Uri { get; }

	/// <summary>
	/// Gets or sets a value indicating whether to cancel the WebView navigation.
	/// </summary>
	public bool Cancel { get; set; }
}
