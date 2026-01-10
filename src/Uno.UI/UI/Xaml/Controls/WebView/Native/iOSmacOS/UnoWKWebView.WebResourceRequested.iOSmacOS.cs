#if __IOS__ || __MACOS__
#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using Foundation;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Uno.Web.WebView2.Core;
using WebKit;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Partial class extension for UnoWKWebView to implement ISupportsWebResourceRequested.
/// </summary>
#if UIKIT_SKIA
internal
#else
public
#endif
	partial class UnoWKWebView : ISupportsWebResourceRequested
{
	private readonly List<WebResourceFilter> _webResourceFilters = new();
	private readonly Dictionary<string, string> _customHeaders = new(StringComparer.OrdinalIgnoreCase);
	private bool _interceptorInjected;

	public event EventHandler<CoreWebView2WebResourceRequestedEventArgs>? WebResourceRequested;

	public void AddWebResourceRequestedFilter(
		string uri,
		CoreWebView2WebResourceContext resourceContext,
		CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		_webResourceFilters.Add(new WebResourceFilter(uri, resourceContext, requestSourceKinds));

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"WebResourceRequested filter added: {uri}");
		}
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
	private void OnWebResourceRequested(NSUrlRequest request)
	{
		var url = request.Url?.AbsoluteString ?? string.Empty;
		var resourceContext = WebResourceContextHelper.DetermineResourceContext(url);

		if (!MatchesAnyFilter(url, resourceContext))
		{
			return;
		}

		var nativeArgs = new NativeCoreWebView2WebResourceRequestedEventArgs(request, resourceContext);
		var args = new CoreWebView2WebResourceRequestedEventArgs(nativeArgs);
		WebResourceRequested?.Invoke(this, args);

		// Track header modifications for JavaScript injection
		if (nativeArgs.HasHeaderModifications)
		{
			var effectiveHeaders = nativeArgs.GetEffectiveHeaders();
			if (effectiveHeaders != null)
			{
				UpdateCustomHeaders(effectiveHeaders);
			}
		}

		// Note: On iOS/macOS, we cannot actually apply header modifications to navigation requests
		// However, we can inject headers into JavaScript-initiated requests via the interceptor
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

	/// <summary>
	/// Injects the JavaScript interceptor that overrides fetch and XMLHttpRequest
	/// to allow custom header injection into AJAX requests.
	/// </summary>
	internal void InjectWebResourceInterceptor()
	{
		if (_interceptorInjected || _webResourceFilters.Count == 0)
		{
			return;
		}

		try
		{
			var script = GetInterceptorScript();
			EvaluateJavaScript(script, (result, error) =>
			{
				if (error != null)
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn($"Failed to inject WebResourceRequested interceptor: {error.LocalizedDescription}");
					}
				}
				else
				{
					_interceptorInjected = true;
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug("WebResourceRequested JavaScript interceptor injected successfully");
					}

					// If we already have custom headers, sync them now
					if (_customHeaders.Count > 0)
					{
						SyncCustomHeadersToJavaScript();
					}
				}
			});
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Failed to inject WebResourceRequested interceptor: {ex.Message}");
			}
		}
	}

	/// <summary>
	/// Updates the custom headers that will be injected into AJAX requests.
	/// </summary>
	private void UpdateCustomHeaders(IDictionary<string, string> headers)
	{
		_customHeaders.Clear();
		foreach (var kvp in headers)
		{
			_customHeaders[kvp.Key] = kvp.Value;
		}

		if (_interceptorInjected)
		{
			SyncCustomHeadersToJavaScript();
		}
	}

	/// <summary>
	/// Syncs the custom headers to the JavaScript context.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dictionary<string, string> serialization is preserved")]
	private void SyncCustomHeadersToJavaScript()
	{
		try
		{
			var headersJson = JsonSerializer.Serialize(_customHeaders);
			var script = $"if(window.__unoSetHeaders){{window.__unoSetHeaders({headersJson});}}";
			EvaluateJavaScript(script, (result, error) =>
			{
				if (error != null && this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Failed to sync headers to JavaScript: {error.LocalizedDescription}");
				}
			});
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Failed to sync headers to JavaScript: {ex.Message}");
			}
		}
	}

	/// <summary>
	/// Gets the JavaScript code that will intercept fetch and XMLHttpRequest.
	/// </summary>
	private static string GetInterceptorScript()
	{
		// This script overrides fetch and XMLHttpRequest to allow header injection
		return @"
(function() {
    if (window.__unoWebResourceInterceptorInstalled) return;
    window.__unoWebResourceInterceptorInstalled = true;

    // Store custom headers to inject
    window.__unoCustomHeaders = {};

    // Set headers from C#
    window.__unoSetHeaders = function(headers) {
        window.__unoCustomHeaders = headers || {};
    };

    // Override fetch
    const originalFetch = window.fetch;
    window.fetch = function(input, init) {
        init = init || {};
        
        // Handle Headers object or plain object
        let headers;
        if (init.headers instanceof Headers) {
            headers = init.headers;
        } else {
            headers = new Headers(init.headers || {});
        }
        
        // Apply custom headers
        for (const [key, value] of Object.entries(window.__unoCustomHeaders)) {
            if (value) {
                headers.set(key, value);
            }
        }
        
        init.headers = headers;
        return originalFetch.call(this, input, init);
    };

    // Override XMLHttpRequest
    const originalOpen = XMLHttpRequest.prototype.open;
    const originalSend = XMLHttpRequest.prototype.send;
    
    XMLHttpRequest.prototype.open = function(method, url, async, user, password) {
        this.__unoUrl = url;
        this.__unoMethod = method;
        this.__unoHeadersApplied = false;
        return originalOpen.apply(this, arguments);
    };
    
    XMLHttpRequest.prototype.send = function(body) {
        // Apply custom headers before send (only once)
        if (!this.__unoHeadersApplied) {
            this.__unoHeadersApplied = true;
            for (const [key, value] of Object.entries(window.__unoCustomHeaders)) {
                if (value) {
                    try {
                        this.setRequestHeader(key, value);
                    } catch (e) {
                        // Some headers cannot be set (e.g., restricted headers)
                        console.warn('Uno WebResourceRequested: Could not set header ' + key + ': ' + e.message);
                    }
                }
            }
        }
        return originalSend.call(this, body);
    };
    
    console.log('Uno WebResourceRequested JavaScript interceptor installed');
})();
";
	}
}
#endif
