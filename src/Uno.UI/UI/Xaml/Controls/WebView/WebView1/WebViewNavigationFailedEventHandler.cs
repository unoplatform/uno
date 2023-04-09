namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Represents the method that will handle the WebView.NavigationFailed event.
/// </summary>
/// <param name="sender">The object where the event handler is attached.</param>
/// <param name="e">The event data.</param>
public delegate void WebViewNavigationFailedEventHandler(
	object sender,
	WebViewNavigationFailedEventArgs e);
