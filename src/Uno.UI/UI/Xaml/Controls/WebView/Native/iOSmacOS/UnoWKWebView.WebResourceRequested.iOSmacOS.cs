#if __IOS__ || __MACOS__
#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Web.WebView2.Core;
using WebKit;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Partial class extension for UnoWKWebView to implement ISupportsWebResourceRequested.
/// 
/// WKWEBVIEW LIMITATIONS:
/// ======================
/// 1. NAVIGATION ACTIONS ONLY:
///    WKNavigationDelegate only intercepts main document navigation,
///    not sub-resource requests (images, scripts, CSS, etc.).
///    
/// 2. HEADER MODIFICATION NOT POSSIBLE:
///    WKWebView does not allow modifying request headers.
///    Header modifications are tracked but NOT applied.
///    
/// 3. NO CUSTOM RESPONSES:
///    Cannot provide custom response content.
///    
/// This implementation is provided for API compatibility and allows:
/// - Reading request information
/// - Logging/tracking requests
/// - Fire events for navigation actions
/// </summary>
internal partial class UnoWKWebView : ISupportsWebResourceRequested
{
	private readonly List<WebResourceFilter> _webResourceFilters = new();

	public event EventHandler<CoreWebView2WebResourceRequestedEventArgs>? WebResourceRequested;

	public void AddWebResourceRequestedFilter(
		string uri,
		CoreWebView2WebResourceContext resourceContext,
		CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		_webResourceFilters.Add(new WebResourceFilter(uri, resourceContext, requestSourceKinds));
	}

	public void RemoveWebResourceRequestedFilter(
		string uri,
		CoreWebView2WebResourceContext resourceContext,
		CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		_webResourceFilters.RemoveAll(f => f.Equals(uri, resourceContext, requestSourceKinds));
	}

	/// <summary>
	/// Called from WKNavigationDelegate when a navigation action is about to occur.
	/// </summary>
	internal void OnWebResourceRequested(WKNavigationAction navigationAction)
	{
		if (_webResourceFilters.Count == 0)
		{
			return;
		}

		var request = navigationAction.Request;
		if (request?.Url == null)
		{
			return;
		}

		OnWebResourceRequested(request);
	}

	/// <summary>
	/// Called to process a web resource request.
	/// </summary>
	private void OnWebResourceRequested(Foundation.NSUrlRequest request)
	{
		var url = request.Url?.AbsoluteString ?? string.Empty;
		var resourceContext = WebResourceContextHelper.DetermineResourceContext(url);

		if (!MatchesAnyFilter(url, resourceContext))
		{
			return;
		}

		var args = new CoreWebView2WebResourceRequestedEventArgs(request, resourceContext);
		WebResourceRequested?.Invoke(this, args);

		// Note: On iOS/macOS, we cannot actually apply header modifications or custom responses
		// The event is fired for API compatibility and logging purposes
	}

	private bool MatchesAnyFilter(string url, CoreWebView2WebResourceContext resourceContext)
	{
		foreach (var filter in _webResourceFilters)
		{
			if (filter.MatchesUrl(url) && filter.MatchesContext(resourceContext))
			{
				return true;
			}
		}
		return false;
	}
}
#endif
