#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;
using Uno.Web.WebView2.Core;
using static __Microsoft.UI.Xaml.Controls.NativeWebView;

#if WASM_SKIA
using ElementId = System.String;
#else
using ElementId = System.IntPtr;
#endif

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Partial class extension for NativeWebView to implement ISupportsWebResourceRequested on WASM.
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
        
        // Handle Headers object or plain object consistently
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

		var nativeArgs = new NativeCoreWebView2WebResourceRequestedEventArgs(url, method, headers, resourceContext);
		var args = new CoreWebView2WebResourceRequestedEventArgs(nativeArgs);
		WebResourceRequested?.Invoke(this, args);

		// If headers were modified, update them in the JavaScript context
		if (nativeArgs.HasHeaderModifications)
		{
			var effectiveHeaders = nativeArgs.GetEffectiveHeaders();
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
