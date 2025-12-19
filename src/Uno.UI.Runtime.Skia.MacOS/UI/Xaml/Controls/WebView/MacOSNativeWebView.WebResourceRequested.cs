#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.MacOS;

/// <summary>
/// macOS Skia implementation of WebResourceRequested using native WKWebView.
/// 
/// IMPLEMENTATION APPROACH:
/// ========================
/// Uses the cancel-and-reload pattern in native code (UNOWebView.m):
/// 1. Native WKWebView decidePolicyForNavigationAction intercepts navigation
/// 2. Calls C# WebResourceRequestedCallback with URL and method
/// 3. C# fires WebResourceRequested event and collects modified headers
/// 4. Returns JSON string of headers to native code
/// 5. Native code cancels navigation and reloads with modified NSMutableURLRequest
/// 
/// LIMITATIONS:
/// - Only main document navigation is intercepted (not sub-resources)
/// - No custom response support (WKWebView limitation)
/// - Each header modification causes navigation to be cancelled and restarted
/// </summary>
internal partial class MacOSNativeWebView : ISupportsWebResourceRequested
{
	private readonly List<WebResourceFilter> _webResourceFilters = new();
	private readonly HashSet<string> _pendingInjectedNavigationKeys = new(StringComparer.OrdinalIgnoreCase);

	public event EventHandler<CoreWebView2WebResourceRequestedEventArgs>? WebResourceRequested;

	public void AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		_webResourceFilters.Add(new WebResourceFilter(uri, resourceContext, requestSourceKinds));

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"WebResourceRequested filter added: {uri}");
		}
	}

	public void RemoveWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContext resourceContext, CoreWebView2WebResourceRequestSourceKinds requestSourceKinds)
	{
		_webResourceFilters.RemoveAll(f => f.Equals(uri, resourceContext, requestSourceKinds));
	}

	/// <summary>
	/// Callback invoked from native code when a navigation action is about to occur.
	/// Returns a JSON string of headers to inject, or null if no modification needed.
	/// </summary>
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	public static unsafe sbyte* WebResourceRequestedCallback(IntPtr handle, sbyte* urlPtr, sbyte* methodPtr)
	{
		try
		{
			if (urlPtr == null || methodPtr == null)
			{
				return null;
			}

			var url = Marshal.PtrToStringUTF8((IntPtr)urlPtr);
			var method = Marshal.PtrToStringUTF8((IntPtr)methodPtr);

			if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(method))
			{
				return null;
			}

			// Find the MacOSNativeWebView instance for this handle
			if (!_webViews.TryGetValue(handle, out var weakRef) || !weakRef.TryGetTarget(out var webView))
			{
				return null;
			}

			return webView.OnWebResourceRequested(url, method);
		}
		catch (Exception ex)
		{
			if (typeof(MacOSNativeWebView).Log().IsEnabled(LogLevel.Error))
			{
				typeof(MacOSNativeWebView).Log().Error($"WebResourceRequestedCallback error: {ex}");
			}
			return null;
		}
	}

	/// <summary>
	/// Processes the web resource request and returns JSON headers if modification is needed.
	/// </summary>
	private unsafe sbyte* OnWebResourceRequested(string url, string method)
	{
		try
		{
			var requestKey = BuildRequestKey(url, method);

			// If we already injected headers for this request, skip interception to avoid infinite reload loops.
			if (_pendingInjectedNavigationKeys.Remove(requestKey))
			{
				return null;
			}

			// Check if any filters match this request
			if (_webResourceFilters.Count == 0 || !MatchesAnyFilter(url))
			{
				return null;
			}

			// Create event args - macOS specific implementation
			var args = new MacOSWebResourceRequestedEventArgs(url, method);

			// Fire the event
			WebResourceRequested?.Invoke(this, args);

			// Check if headers were modified
			if (!args.HasModifiedHeaders)
			{
				return null;
			}

			// Serialize modified headers to JSON
			var headers = args.GetModifiedHeaders();
			if (headers == null || headers.Count == 0)
			{
				return null;
			}

			_pendingInjectedNavigationKeys.Add(requestKey);

			var json = JsonSerializer.Serialize(headers);

			// Allocate unmanaged string - native code will free it
			return (sbyte*)Marshal.StringToHGlobalAnsi(json);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"OnWebResourceRequested error: {ex}");
			}
			return null;
		}
	}

	private bool MatchesAnyFilter(string url)
	{
		foreach (var filter in _webResourceFilters)
		{
			if (filter.MatchesUrl(url))
			{
				return true;
			}
		}
		return false;
	}

	private static string BuildRequestKey(string url, string method)
		=> $"{method}:{url}";
}

/// <summary>
/// macOS-specific implementation of CoreWebView2WebResourceRequestedEventArgs.
/// Provides a simple header tracking mechanism without wrapping native objects.
/// </summary>
internal class MacOSWebResourceRequestedEventArgs : CoreWebView2WebResourceRequestedEventArgs
{
	private readonly MacOSWebResourceRequest _request;
	private readonly Dictionary<string, string> _modifiedHeaders = new();

	public MacOSWebResourceRequestedEventArgs(string url, string method)
		: base(nativeArgs: new MacOSNativeArgsStub(url, method))
	{
		_request = new MacOSWebResourceRequest(url, method, this);

		// Inject our custom request into the base class's _request field
		InjectCustomRequest();
	}

	private void InjectCustomRequest()
	{
		try
		{
			var requestField = typeof(CoreWebView2WebResourceRequestedEventArgs).GetField(
				"_request",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

			requestField?.SetValue(this, _request);

			var headersField = typeof(CoreWebView2WebResourceRequest).GetField(
				"_headers",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

			headersField?.SetValue(_request, _request.GetMacOSHeaders());
		}
		catch (Exception ex)
		{
			if (typeof(MacOSWebResourceRequestedEventArgs).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(MacOSWebResourceRequestedEventArgs).Log().Warn($"Failed to inject custom request: {ex.Message}");
			}
		}
	}

	internal bool HasModifiedHeaders => _modifiedHeaders.Count > 0;

	internal Dictionary<string, string>? GetModifiedHeaders() => _modifiedHeaders.Count > 0 ? _modifiedHeaders : null;

	internal void TrackHeaderChange(string name, string value)
	{
		_modifiedHeaders[name] = value;
	}

	internal void TrackHeaderRemoval(string name)
	{
		_modifiedHeaders[name] = string.Empty;
	}

	/// <summary>
	/// Stub native args object that provides native-like request/response objects.
	/// </summary>
	private class MacOSNativeArgsStub
	{
		private readonly MacOSDynamicRequestStub _nativeRequest;

		public MacOSNativeArgsStub(string url, string method)
		{
			_nativeRequest = new MacOSDynamicRequestStub(url, method);
		}

		public MacOSDynamicRequestStub Request => _nativeRequest;
		public object? Response { get; set; }
		public int ResourceContext => (int)CoreWebView2WebResourceContext.Document;
		public int RequestedSourceKind => (int)CoreWebView2WebResourceRequestSourceKinds.Document;
	}
}

/// <summary>
/// macOS-specific implementation of CoreWebView2WebResourceRequest.
/// </summary>
internal class MacOSWebResourceRequest : CoreWebView2WebResourceRequest
{
	private readonly string _uri;
	private readonly string _method;
	private readonly MacOSHttpRequestHeaders _headers;
	private readonly MacOSWebResourceRequestedEventArgs _eventArgs;

	public MacOSWebResourceRequest(string uri, string method, MacOSWebResourceRequestedEventArgs eventArgs)
		: base(nativeRequest: new MacOSDynamicRequestStub(uri, method))
	{
		_uri = uri;
		_method = method;
		_eventArgs = eventArgs;
		_headers = new MacOSHttpRequestHeaders(this);
	}

	public new string Uri
	{
		get => _uri;
		set { } // Read-only for macOS
	}

	public new string Method
	{
		get => _method;
		set { } // Read-only for macOS
	}

	public new CoreWebView2HttpRequestHeaders Headers => _headers;

	internal MacOSWebResourceRequestedEventArgs EventArgs => _eventArgs;

	internal MacOSHttpRequestHeaders GetMacOSHeaders() => _headers;
}

/// <summary>
/// macOS-specific implementation of CoreWebView2HttpRequestHeaders.
/// </summary>
internal class MacOSHttpRequestHeaders : CoreWebView2HttpRequestHeaders
{
	private readonly MacOSWebResourceRequest _request;
	private readonly Dictionary<string, string> _headers = new();

	public MacOSHttpRequestHeaders(MacOSWebResourceRequest request)
		: base(nativeHeaders: new MacOSNativeHeadersStub(request))
	{
		_request = request;
	}

	public override string GetHeader(string name)
	{
		return _headers.TryGetValue(name, out var value) ? value : string.Empty;
	}

	public override bool Contains(string name)
	{
		return _headers.ContainsKey(name);
	}

	public override void SetHeader(string name, string value)
	{
		_headers[name] = value;
		_request.EventArgs.TrackHeaderChange(name, value);
	}

	public override void RemoveHeader(string name)
	{
		_headers.Remove(name);
		_request.EventArgs.TrackHeaderRemoval(name);
	}
}

/// <summary>
/// Stub native headers object that delegates to the request's EventArgs for tracking.
/// Must be internal so the dynamic runtime binder can access its members.
/// </summary>
internal class MacOSNativeHeadersStub
{
	private readonly MacOSWebResourceRequest _request;
	private readonly Dictionary<string, string> _headers = new();

	public MacOSNativeHeadersStub(MacOSWebResourceRequest request)
	{
		_request = request;
	}

	public string GetHeader(string name)
	{
		return _headers.TryGetValue(name, out var value) ? value : string.Empty;
	}

	public bool Contains(string name) => _headers.ContainsKey(name);

	public void SetHeader(string name, string value)
	{
		_headers[name] = value;
		_request.EventArgs.TrackHeaderChange(name, value);
	}

	public void RemoveHeader(string name)
	{
		_headers.Remove(name);
		_request.EventArgs.TrackHeaderRemoval(name);
	}
}

/// <summary>
/// Dynamic binder-friendly stub representing a native request.
/// Must be public so the dynamic binder can access its members.
/// </summary>
public sealed class MacOSDynamicRequestStub
{
	public MacOSDynamicRequestStub(string uri, string method)
	{
		Uri = uri;
		Method = method;
		Headers = new object();
	}

	public string Uri { get; set; }
	public string Method { get; set; }
	public object Headers { get; set; }
	public object? Content { get; set; }
}
