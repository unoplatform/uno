#nullable enable

using CoreGraphics;
using Foundation;
using System;
using WebKit;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Web;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Uno.UI.Xaml.Controls;
using System.Net.Http;
using Microsoft.Web.WebView2.Core;
using System.Collections.Generic;
using Windows.Foundation;
using System.Globalization;
using Windows.UI.Core;

#if __APPLE_UIKIT__
using UIKit;
#else
using AppKit;
#endif

namespace Microsoft.UI.Xaml.Controls;

internal class WebViewNavigationDelegate : WKNavigationDelegate
{
	/// <summary>
	/// The reference to the parent UnoWKWebView class on which we invoke callbacks.
	/// </summary>
	private readonly WeakReference<UnoWKWebView> _unoWKWebView;

	public WebViewNavigationDelegate(UnoWKWebView unoWKWebView)
	{
		_unoWKWebView = new WeakReference<UnoWKWebView>(unoWKWebView);
	}

	public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
	{
		var requestUrl = navigationAction.Request?.Url.ToUri();

		if (_unoWKWebView.TryGetTarget(out var unoWKWebView))
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"WKNavigationDelegate.DecidePolicy: NavigationType: {navigationAction.NavigationType} Request:{requestUrl} TargetRequest: {navigationAction.TargetFrame?.Request}");
			}

			var scheme = requestUrl?.Scheme ?? "";

			// Note that the "file" scheme is not officially supported by the UWP WebView (https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.webview.unsupportedurischemeidentified?view=winrt-19041#remarks).
			// We have to support it here for anchor navigation (as long as https://github.com/unoplatform/uno/issues/2998 is not resolved).
			var isUnsupportedScheme = !scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && !scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && !scheme.Equals("file", StringComparison.OrdinalIgnoreCase);
			if (isUnsupportedScheme)
			{
				bool cancelled = unoWKWebView.OnUnsupportedUriSchemeIdentified(requestUrl!);
				decisionHandler(cancelled ? WKNavigationActionPolicy.Cancel : WKNavigationActionPolicy.Allow);
				return;
			}

			// Check for anchor navigation first, before processing as a regular navigation
			if (requestUrl != null && GetIsAnchorNavigation(requestUrl))
			{
				unoWKWebView.OnAnchorNavigation(requestUrl);
				decisionHandler(WKNavigationActionPolicy.Allow);
				return;
			}

			// For link activations, check navigation start for non-anchor navigations
			if (navigationAction.NavigationType == WKNavigationType.LinkActivated)
			{
				if (unoWKWebView.OnStarted(requestUrl, stopLoadingOnCanceled: true))
				{
					decisionHandler(WKNavigationActionPolicy.Cancel);
					return;
				}
			}

			// For all other cases, allow the navigation
			decisionHandler(WKNavigationActionPolicy.Allow);
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
			{
				this.Log().LogWarning($"WKNavigationDelegate.DecidePolicy: Cancelling navigation because owning WKWebView is null (NavigationType: {navigationAction.NavigationType} Request:{requestUrl} TargetRequest: {navigationAction.TargetFrame?.Request})");
			}

			decisionHandler(WKNavigationActionPolicy.Cancel);
		}
	}

	private bool GetIsAnchorNavigation(Uri requestUrl)
	{
		if (!_unoWKWebView.TryGetTarget(out var unoWKWebView))
		{
			return false;
		}

		if (unoWKWebView._lastNavigationData is not Uri lastNavigationUri)
		{
			return false;
		}

		return WebViewUtils.IsAnchorNavigation(lastNavigationUri.AbsoluteUri, requestUrl.AbsoluteUri);
	}

	public override void DidStartProvisionalNavigation(WKWebView webView, WKNavigation navigation)
	{
		if (_unoWKWebView.TryGetTarget(out var unoWKWebView))
		{
			var uri = webView.Url?.ToUri();
			// Only raise NavigationStarting if it hasn't been raised yet and it's not an anchor navigation
			if (uri != null &&
				(unoWKWebView._lastNavigationData is null || !uri.Equals(unoWKWebView._lastNavigationData)) &&
				!GetIsAnchorNavigation(uri))
			{
				unoWKWebView.OnStarted(uri);
			}
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"WKNavigationDelegate.DidStartProvisionalNavigation: Ignoring because owning WKWebView is null");
			}
		}
	}

	public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
	{
		var url = webView.Url?.ToUri();
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKNavigationDelegate.DidFinishNavigation: Request:{url}");
		}

		if (_unoWKWebView.TryGetTarget(out var unoWKWebView))
		{
			unoWKWebView.OnNavigationFinished(url);
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"WKNavigationDelegate.DidFinishNavigation: Ignoring because owning WKWebView is null");
			}
		}
	}

	public override void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error)
	{
		if (_unoWKWebView.TryGetTarget(out var unoWKWebView))
		{
			unoWKWebView.OnError(webView, navigation, error);
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"WKNavigationDelegate.DidFailNavigation: Ignoring because owning WKWebView is null");
			}
		}
	}

	public override void DidFailProvisionalNavigation(WKWebView webView, WKNavigation navigation, NSError error)
	{
		if (_unoWKWebView.TryGetTarget(out var unoWKWebView))
		{
			unoWKWebView.OnError(webView, navigation, error);
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"WKNavigationDelegate.DidFailProvisionalNavigation: Ignoring because owning WKWebView is null");
			}
		}
	}

	public override void DidReceiveServerRedirectForProvisionalNavigation(WKWebView webView, WKNavigation navigation)
	{
		if (_unoWKWebView.TryGetTarget(out var unoWKWebView))
		{
			unoWKWebView.OnStarted(webView.Url?.ToUri());
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"WKNavigationDelegate.DidReceiveServerRedirectForProvisionalNavigation: Ignoring because owning WKWebView is null");
			}
		}
	}

	public override void DecidePolicy(WKWebView webView, WKNavigationResponse navigationResponse, Action<WKNavigationResponsePolicy> decisionHandler)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKNavigationDelegate.DecidePolicy {navigationResponse.Response?.Url?.ToUri()}");
		}

		decisionHandler(WKNavigationResponsePolicy.Allow);
	}

	[Obsolete("https://github.com/unoplatform/uno/pull/1591")]
	internal static bool MustUseWebKitWebView() => true;
}
