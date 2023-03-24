using System;
using System.Globalization;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Android.Webkit;
using Microsoft.Web.WebView2.Core;
using Windows.UI.Xaml.Controls;
using Windows.Web;

namespace Uno.UI.Xaml.Controls;

internal class InternalClient : Android.Webkit.WebViewClient
{
	private readonly CoreWebView2 _coreWebView;
	private readonly NativeWebViewWrapper _nativeWebViewWrapper;
#pragma warning disable CS0414 //TODO:MZ:
	//_owner is because we go through onReceivedError() and OnPageFinished() when the call fail.
	private bool _coreWebViewSuccess = true;
	//_owner is to not have duplicate event call
	private WebErrorStatus _webErrorStatus = WebErrorStatus.Unknown;

	internal InternalClient(CoreWebView2 coreWebView, NativeWebViewWrapper webViewWrapper)
	{
		_coreWebView = coreWebView;
		_nativeWebViewWrapper = webViewWrapper;
	}

#pragma warning disable CS0672 // Member overrides obsolete member
	public override bool ShouldOverrideUrlLoading(Android.Webkit.WebView view, string url)
#pragma warning restore CS0672 // Member overrides obsolete member
	{
		if (url.StartsWith(Uri.UriSchemeMailto, true, CultureInfo.InvariantCulture))
		{
			_nativeWebViewWrapper.CreateAndLaunchMailtoIntent(view.Context, url);
			return true;
		}

		_coreWebView.RaiseNavigationStarting(url, out var cancel);

		return cancel;
	}

	public override void OnPageStarted(Android.Webkit.WebView view, string url, Bitmap favicon)
	{
		base.OnPageStarted(view, url, favicon);
		//Reset Webview Success on page started so that if we have successful navigation we don't send an webView error if a previous error happened.
		_coreWebViewSuccess = true;
	}

#pragma warning disable 0672, 618
	public override void OnReceivedError(Android.Webkit.WebView view, [GeneratedEnum] ClientError errorCode, string description, string failingUrl)
	{
		_coreWebViewSuccess = false;
		_webErrorStatus = ConvertClientError(errorCode);

		base.OnReceivedError(view, errorCode, description, failingUrl);
	}
#pragma warning restore 0672, 618

	public override void OnPageFinished(Android.Webkit.WebView view, string url)
	{
		_coreWebView.DocumentTitle = view.Title;

		_coreWebView.RaiseHistoryChanged();

		var uri = !_nativeWebViewWrapper._wasLoadedFromString && !string.IsNullOrEmpty(url) ? new Uri(url) : null;

		_coreWebView.RaiseNavigationCompleted(uri, true, 200, CoreWebView2WebErrorStatus.Unknown);
		base.OnPageFinished(view, url);
	}

	//Matched using these two sources
	//http://developer.xamarin.com/api/type/Android.Webkit.ClientError/
	//https://msdn.microsoft.com/en-ca/library/windows/apps/windows.web.weberrorstatus
	private WebErrorStatus ConvertClientError(ClientError clientError)
	{
		switch (clientError)
		{
			case ClientError.Authentication:
				return WebErrorStatus.Unauthorized;
			case ClientError.BadUrl:
				return WebErrorStatus.BadRequest;
			case ClientError.Connect:
				return WebErrorStatus.CannotConnect;
			case ClientError.FailedSslHandshake:
				return WebErrorStatus.UnexpectedClientError;
			case ClientError.File:
				return WebErrorStatus.UnexpectedClientError;
			case ClientError.FileNotFound:
				return WebErrorStatus.NotFound;
			case ClientError.HostLookup:
				return WebErrorStatus.HostNameNotResolved;
			case ClientError.Io:
				return WebErrorStatus.InternalServerError;
			case ClientError.ProxyAuthentication:
				return WebErrorStatus.ProxyAuthenticationRequired;
			case ClientError.RedirectLoop:
				return WebErrorStatus.RedirectFailed;
			case ClientError.Timeout:
				return WebErrorStatus.Timeout;
			case ClientError.TooManyRequests:
				return WebErrorStatus.UnexpectedClientError;
			case ClientError.Unknown:
				return WebErrorStatus.Unknown;
			case ClientError.UnsupportedAuthScheme:
				return WebErrorStatus.Unauthorized;
			case ClientError.UnsupportedScheme:
				return WebErrorStatus.UnexpectedClientError;
			default:
				return WebErrorStatus.UnexpectedClientError;
		}
	}
}
