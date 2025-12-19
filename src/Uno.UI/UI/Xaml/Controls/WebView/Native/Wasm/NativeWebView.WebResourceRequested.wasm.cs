#if __WASM__
#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;
using static __Microsoft.UI.Xaml.Controls.NativeWebView;

#if WASM_SKIA
using ElementId = System.String;
#else
using ElementId = System.IntPtr;
#endif

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Partial class extension for NativeWebView to implement ISupportsWebResourceRequested on WASM.
/// 
/// WASM IMPLEMENTATION:
/// ====================
/// Uses JavaScript injection to override window.fetch and XMLHttpRequest.prototype
/// to intercept JavaScript-initiated requests and inject custom headers.
/// 
/// LIMITATIONS:
/// - Only fetch/XMLHttpRequest requests are intercepted
/// - HTML elements (img, script, link, etc.) are NOT intercepted  
/// - Same-origin policy applies
/// - Cross-origin iframes cannot be controlled
/// </summary>
internal partial class NativeWebView : ISupportsWebResourceRequested
{
	private readonly List<WebResourceFilter> _wasmResourceFilters = new();
	private bool _interceptorInjected;

	public event EventHandler<CoreWebView2WebResourceRequestedEventArgs>? WebResourceRequested;

	public void AddWebResourceRequestedFilter(
		string uri,
		CoreWebView2WebResourceContext resourceContext,
		CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		_wasmResourceFilters.Add(new WebResourceFilter(uri, resourceContext, requestSourceKinds));
		EnsureInterceptorInjected();
	}

	public void RemoveWebResourceRequestedFilter(
		string uri,
		CoreWebView2WebResourceContext resourceContext,
		CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		_wasmResourceFilters.RemoveAll(f => f.Equals(uri, resourceContext, requestSourceKinds));
	}

	private void EnsureInterceptorInjected()
	{
		if (_interceptorInjected || _wasmResourceFilters.Count == 0)
		{
			return;
		}

		try
		{
			// Inject JavaScript to intercept fetch and XMLHttpRequest
			var script = GetInterceptorScript();
			NativeMethods.ExecuteScript(_elementId, script);
			_interceptorInjected = true;
		}
		catch
		{
			// Injection may fail for cross-origin iframes
		}
	}

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
        init.headers = init.headers || {};
        
        // Apply custom headers
        for (const [key, value] of Object.entries(window.__unoCustomHeaders)) {
            if (init.headers instanceof Headers) {
                init.headers.set(key, value);
            } else {
                init.headers[key] = value;
            }
        }
        
        return originalFetch.call(this, input, init);
    };

    // Override XMLHttpRequest
    const originalOpen = XMLHttpRequest.prototype.open;
    const originalSend = XMLHttpRequest.prototype.send;
    
    XMLHttpRequest.prototype.open = function(method, url) {
        this.__unoUrl = url;
        this.__unoMethod = method;
        return originalOpen.apply(this, arguments);
    };
    
    XMLHttpRequest.prototype.send = function(body) {
        // Apply custom headers before send
        for (const [key, value] of Object.entries(window.__unoCustomHeaders)) {
            try {
                this.setRequestHeader(key, value);
            } catch (e) {
                // Some headers cannot be set
            }
        }
        return originalSend.call(this, body);
    };
})();
";
	}

	/// <summary>
	/// Updates the custom headers in the iframe's JavaScript context.
	/// </summary>
	internal void UpdateCustomHeaders(IDictionary<string, string> headers)
	{
		if (!_interceptorInjected)
		{
			return;
		}

		try
		{
			var headersJson = JsonSerializer.Serialize(headers);
			var script = $"window.__unoSetHeaders({headersJson});";
			NativeMethods.ExecuteScript(_elementId, script);
		}
		catch
		{
			// May fail for cross-origin iframes
		}
	}

	/// <summary>
	/// Called when a web resource request is detected.
	/// On WASM, this would be triggered by JavaScript interop callbacks.
	/// </summary>
	internal CoreWebView2WebResourceRequestedEventArgs? OnWasmWebResourceRequested(
		string url,
		string method,
		IDictionary<string, string>? headers)
	{
		var resourceContext = WebResourceContextHelper.DetermineResourceContext(url);

		if (!MatchesAnyWasmFilter(url, resourceContext))
		{
			return null;
		}

		var args = new CoreWebView2WebResourceRequestedEventArgs(url, method, headers, resourceContext);
		WebResourceRequested?.Invoke(this, args);

		// If headers were modified, update them in the JavaScript context
		if (args.HasHeaderModifications)
		{
			var effectiveHeaders = args.GetEffectiveHeaders();
			if (effectiveHeaders != null)
			{
				UpdateCustomHeaders(effectiveHeaders);
			}
		}

		return args;
	}

	private bool MatchesAnyWasmFilter(string url, CoreWebView2WebResourceContext resourceContext)
	{
		foreach (var filter in _wasmResourceFilters)
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
