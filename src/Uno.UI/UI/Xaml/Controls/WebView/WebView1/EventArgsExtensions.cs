using Microsoft.Web.WebView2.Core;

namespace Windows.UI.Xaml.Controls;

internal static class EventArgsExtensions
{
	public static WebViewContentLoadingEventArgs ToWebViewArgs(this CoreWebView2ContentLoadingEventArgs args) =>
		new WebViewContentLoadingEventArgs(args.Uri);

	public static WebViewDOMContentLoadedEventArgs ToWebViewArgs(this CoreWebView2DOMContentLoadedEventArgs args) =>
		new WebViewDOMContentLoadedEventArgs(args.Uri);

	public static WebViewNavigationStartingEventArgs ToWebViewArgs(this CoreWebView2NavigationStartingEventArgs args) =>
		new WebViewNavigationStartingEventArgs(args.Uri);

	public static WebViewNavigationCompletedEventArgs ToWebViewArgs(this CoreWebView2NavigationCompletedEventArgs args) =>
		new WebViewNavigationCompletedEventArgs(args.IsSuccess, args.Uri, Web.WebErrorStatus.Unknown); //TODO:MZ:

	public static WebViewNewWindowRequestedEventArgs ToWebViewArgs(this CoreWebView2NewWindowRequestedEventArgs args) =>
		new WebViewNewWindowRequestedEventArgs(null, null); //TODO:MZ:
}
