using System;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides data for the WebView.NewWindowRequested event.
/// </summary>
public sealed partial class WebViewNewWindowRequestedEventArgs
{
	internal WebViewNewWindowRequestedEventArgs(Uri referrer, Uri uri)
	{
		Referrer = referrer;
		Uri = uri;
	}

	/// <summary>
	/// Gets or sets a value that marks the routed event as handled. A true value
	/// for Handled prevents other handlers along the event route from handling the same event again.
	/// </summary>
	public Uri Referrer { get; }

	/// <summary>
	/// Gets the Uniform Resource Identifier (URI) of the content the WebView is attempting to navigate to.
	/// </summary>
	public Uri Uri { get; }

	/// <summary>
	/// Gets or sets a value that marks the routed event as handled. A true value
	/// for Handled prevents other handlers along the event route from handling the same event again.
	/// </summary>
	public bool Handled { get; set; }
}
