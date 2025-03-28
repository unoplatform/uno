using Microsoft.Web.WebView2.Core;
using Windows.Web;

namespace Windows.UI.Xaml.Controls;

internal static class WebViewExtensions
{
	public static WebViewNavigationStartingEventArgs ToWebViewArgs(this CoreWebView2NavigationStartingEventArgs args) =>
		new WebViewNavigationStartingEventArgs(args.Uri?.Length <= 2048 ? new global::System.Uri(args.Uri) : CoreWebView2.BlankUri)
		{
			Cancel = args.Cancel
		};

	public static WebViewNavigationCompletedEventArgs ToWebViewArgs(this CoreWebView2NavigationCompletedEventArgs args) =>
		new WebViewNavigationCompletedEventArgs(args.IsSuccess, args.Uri, args.WebErrorStatus.ToWebErrorStatus());

	public static WebViewNewWindowRequestedEventArgs ToWebViewArgs(this CoreWebView2NewWindowRequestedEventArgs args) =>
		new WebViewNewWindowRequestedEventArgs(null, args.Uri?.Length <= 2048 ? new global::System.Uri(args.Uri) : CoreWebView2.BlankUri)
		{
			Handled = args.Handled
		};

	public static WebErrorStatus ToWebErrorStatus(this CoreWebView2WebErrorStatus coreWebView2WebErrorStatus) =>
		coreWebView2WebErrorStatus switch
		{
			CoreWebView2WebErrorStatus.Unknown => WebErrorStatus.Unknown,
			CoreWebView2WebErrorStatus.CertificateCommonNameIsIncorrect => WebErrorStatus.CertificateCommonNameIsIncorrect,
			CoreWebView2WebErrorStatus.CertificateExpired => WebErrorStatus.CertificateExpired,
			CoreWebView2WebErrorStatus.ClientCertificateContainsErrors => WebErrorStatus.CertificateContainsErrors,
			CoreWebView2WebErrorStatus.CertificateRevoked => WebErrorStatus.CertificateRevoked,
			CoreWebView2WebErrorStatus.CertificateIsInvalid => WebErrorStatus.CertificateIsInvalid,
			CoreWebView2WebErrorStatus.ServerUnreachable => WebErrorStatus.ServerUnreachable,
			CoreWebView2WebErrorStatus.Timeout => WebErrorStatus.Timeout,
			CoreWebView2WebErrorStatus.ErrorHttpInvalidServerResponse => WebErrorStatus.ErrorHttpInvalidServerResponse,
			CoreWebView2WebErrorStatus.ConnectionAborted => WebErrorStatus.ConnectionAborted,
			CoreWebView2WebErrorStatus.ConnectionReset => WebErrorStatus.ConnectionReset,
			CoreWebView2WebErrorStatus.Disconnected => WebErrorStatus.Disconnected,
			CoreWebView2WebErrorStatus.CannotConnect => WebErrorStatus.CannotConnect,
			CoreWebView2WebErrorStatus.HostNameNotResolved => WebErrorStatus.HostNameNotResolved,
			CoreWebView2WebErrorStatus.OperationCanceled => WebErrorStatus.OperationCanceled,
			CoreWebView2WebErrorStatus.RedirectFailed => WebErrorStatus.RedirectFailed,
			CoreWebView2WebErrorStatus.UnexpectedError => WebErrorStatus.UnexpectedServerError,
			CoreWebView2WebErrorStatus.ValidAuthenticationCredentialsRequired => WebErrorStatus.Forbidden,
			CoreWebView2WebErrorStatus.ValidProxyAuthenticationRequired => WebErrorStatus.ProxyAuthenticationRequired,
			_ => WebErrorStatus.Unknown
		};
}
