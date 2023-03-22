#nullable enable

using System;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides data for the WebView.ContentLoading event.
/// </summary>
public partial class WebViewContentLoadingEventArgs
{
	internal WebViewContentLoadingEventArgs(Uri? uri) => Uri = uri;

	/// <summary>
	/// Gets the Uniform Resource Identifier (URI) of the content the WebView is loading.
	/// </summary>
	public Uri? Uri { get; }
}
