namespace Microsoft.Web.WebView2.Core;

// Data projected from the native WebView2 controller, not Microsoft UI XAML implementation.
public partial class CoreWebView2MoveFocusRequestedEventArgs
{
	internal CoreWebView2MoveFocusRequestedEventArgs(CoreWebView2MoveFocusReason reason) => Reason = reason;

	/// <summary>Gets or sets whether the focus movement request is handled.</summary>
	public bool Handled { get; set; }

	/// <summary>Gets the reason for the focus movement request.</summary>
	public CoreWebView2MoveFocusReason Reason { get; }
}
