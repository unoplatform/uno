#nullable enable

using System;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides data for the DOMContentLoaded event.
/// </summary>
public partial class WebViewDOMContentLoadedEventArgs
{
	internal WebViewDOMContentLoadedEventArgs(Uri uri) => Uri = uri;

	/// <summary>
	/// Gets the Uniform Resource Identifier (URI) of the content the WebView is loading.
	/// </summary>
	public Uri Uri { get; }
}
