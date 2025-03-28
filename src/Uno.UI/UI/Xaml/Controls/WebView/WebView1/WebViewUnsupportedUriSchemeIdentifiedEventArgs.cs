using System;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides data for the WebView.UnsupportedUriSchemeIdentified event.
/// </summary>
public sealed partial class WebViewUnsupportedUriSchemeIdentifiedEventArgs
{
	internal WebViewUnsupportedUriSchemeIdentifiedEventArgs(Uri uri) =>
		Uri = uri;

	/// <summary>
	/// Gets or sets a value that marks the routed event as handled. A true value
	/// for Handled prevents other handlers along the event route from handling the same event again.
	/// </summary>
	public bool Handled { get; set; }

	/// <summary>
	/// Gets the Uniform Resource Identifier (URI) of the content the WebView attempted to navigate to.
	/// </summary>
	public Uri Uri { get; }
}
