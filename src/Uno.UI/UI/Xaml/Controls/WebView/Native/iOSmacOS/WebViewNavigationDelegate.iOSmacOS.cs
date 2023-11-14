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
using Uno.UI.Extensions;
using System.Collections.Generic;
using Windows.Foundation;
using System.Globalization;
using Windows.UI.Core;

#if __IOS__
using UIKit;
#else
using AppKit;
#endif

namespace Windows.UI.Xaml.Controls;

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

			var scheme = requestUrl.Scheme;

			// Note that the "file" scheme is not officially supported by the UWP WebView (https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.webview.unsupportedurischemeidentified?view=winrt-19041#remarks).
			// We have to support it here for anchor navigation (as long as https://github.com/unoplatform/uno/issues/2998 is not resolved).
			var isUnsupportedScheme = !scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && !scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && !scheme.Equals("file", StringComparison.OrdinalIgnoreCase);
			if (isUnsupportedScheme)
			{
				bool cancelled = unoWKWebView.OnUnsupportedUriSchemeIdentified(requestUrl);

				decisionHandler(cancelled ? WKNavigationActionPolicy.Cancel : WKNavigationActionPolicy.Allow);

				return;
			}

			// The WKWebView doesn't raise navigation event for anchor navigation.
			// When we detect anchor navigation, we must raise the events (NavigationStarting & NavigationFinished) ourselves.
			var isAnchorNavigation = GetIsAnchorNavigation();
			if (isAnchorNavigation)
			{
				bool cancelled = unoWKWebView.OnStarted(requestUrl, stopLoadingOnCanceled: false);

				decisionHandler(cancelled ? WKNavigationActionPolicy.Cancel : WKNavigationActionPolicy.Allow);

				if (!cancelled)
				{
					unoWKWebView.OnNavigationFinished(requestUrl);
				}

				return;
			}

			// For all other cases, we allow the navigation. This will results in other WKNavigationDelegate methods being called.
			decisionHandler(WKNavigationActionPolicy.Allow);

			bool GetIsAnchorNavigation()
			{
				// If we navigate to the exact same page but with a different location (using anchors), the native control will not notify us of
				// any navigation. We need to create this notification to indicate that the navigation worked.

				// To detect an anchor navigation, both the previous and new urls need to match on the left part of the anchor indicator ("#")
				// AND the new url needs to have content on the right of the anchor indicator.
				if (unoWKWebView._lastNavigationData is Uri urlLastNavigation)
				{
					var currentUrlParts = urlLastNavigation?.AbsoluteUri?.ToString().Split('#');
					var newUrlParts = requestUrl?.AbsoluteUri?.ToString().Split('#');

					return currentUrlParts?.Length > 0
						&& newUrlParts?.Length > 1
						&& currentUrlParts[0].Equals(newUrlParts[0]);
				}
				else
				{
					return false;
				}
			}
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
			{
				this.Log().LogWarning($"WKNavigationDelegate.DecidePolicy: Cancelling navigation because owning WKWebView is null (NavigationType: {navigationAction.NavigationType} Request:{requestUrl} TargetRequest: {navigationAction.TargetFrame?.Request})");
			}

			// CancellationToken the navigation, we're in a case where the owning WKWebView is not alive anymore
			decisionHandler(WKNavigationActionPolicy.Cancel);
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

	public override void DidReceiveServerRedirectForProvisionalNavigation(WKWebView webView, WKNavigation navigation)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKNavigationDelegate.DidReceiveServerRedirectForProvisionalNavigation: Request:{webView.Url?.ToUri()}");
		}

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

	public override void ContentProcessDidTerminate(WKWebView webView)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKNavigationDelegate.ContentProcessDidTerminate: Request:{webView.Url?.ToUri()}");
		}
	}

	public override void DidCommitNavigation(WKWebView webView, WKNavigation navigation)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKNavigationDelegate.DidCommitNavigation: Request:{webView.Url?.ToUri()}");
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
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKNavigationDelegate.DidCommitNavigation: Request:{webView.Url?.ToUri()}");
		}

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
	public override void DidStartProvisionalNavigation(WKWebView webView, WKNavigation navigation)
	{
		if (_unoWKWebView.TryGetTarget(out var unoWKWebView))
		{
			unoWKWebView.OnStarted(webView.Url?.ToUri());
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"WKNavigationDelegate.DidStartProvisionalNavigation: Ignoring because owning WKWebView is null");
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

	[Obsolete("https://github.com/unoplatform/uno/pull/1591")]
	internal static bool MustUseWebKitWebView() => true;
}
