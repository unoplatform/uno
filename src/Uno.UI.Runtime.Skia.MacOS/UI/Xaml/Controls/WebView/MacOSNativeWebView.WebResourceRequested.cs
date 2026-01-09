#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Uno.UI.Runtime.Skia.MacOS;

/// <summary>
/// macOS Skia implementation of WebResourceRequested using native WKWebView.
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

internal class MacOSWebResourceRequestedEventArgs : CoreWebView2WebResourceRequestedEventArgs
{
	public MacOSWebResourceRequestedEventArgs(string url, string method)
		: base(new MacOSNativeWebResourceRequestedEventArgs(url, method))
	{
	}

	internal bool HasModifiedHeaders => (NativeArgs as MacOSNativeWebResourceRequestedEventArgs)!.HasModifiedHeaders;

	internal Dictionary<string, string>? GetModifiedHeaders()
		=> (NativeArgs as MacOSNativeWebResourceRequestedEventArgs)!.GetModifiedHeaders();
}

internal class MacOSNativeWebResourceRequestedEventArgs : INativeWebResourceRequestedEventArgs
{
	private readonly MacOSNativeWebResourceRequest _request;
	private INativeWebResourceResponse? _response;

	public MacOSNativeWebResourceRequestedEventArgs(string url, string method)
	{
		_request = new MacOSNativeWebResourceRequest(url, method, this);
	}

	public INativeWebResourceRequest Request => _request;
	public INativeWebResourceResponse? Response { get => _response; set => _response = value; }
	public CoreWebView2WebResourceContext ResourceContext => CoreWebView2WebResourceContext.Document;
	public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind => CoreWebView2WebResourceRequestSourceKinds.Document;

	public Deferral GetDeferral() => new Deferral(() => { });

	private readonly Dictionary<string, string> _modifiedHeaders = new();

	internal void TrackHeaderChange(string name, string value) => _modifiedHeaders[name] = value;
	internal void TrackHeaderRemoval(string name) => _modifiedHeaders[name] = string.Empty;

	internal bool HasModifiedHeaders => _modifiedHeaders.Count > 0;
	internal Dictionary<string, string>? GetModifiedHeaders() => _modifiedHeaders.Count > 0 ? _modifiedHeaders : null;
}

internal class MacOSNativeWebResourceRequest : INativeWebResourceRequest
{
	private readonly string _uri;
	private readonly string _method;
	private readonly MacOSNativeHttpRequestHeaders _headers;

	public MacOSNativeWebResourceRequest(string uri, string method, MacOSNativeWebResourceRequestedEventArgs owner)
	{
		_uri = uri;
		_method = method;
		_headers = new MacOSNativeHttpRequestHeaders(owner);
	}

	public string Uri { get => _uri; set { } }
	public string Method { get => _method; set { } }
	public IRandomAccessStream Content { get => null!; set { } }
	public INativeHttpRequestHeaders Headers => _headers;
}

internal class MacOSNativeHttpRequestHeaders : INativeHttpRequestHeaders
{
	private readonly MacOSNativeWebResourceRequestedEventArgs _owner;
	private readonly Dictionary<string, string> _headers = new();

	public MacOSNativeHttpRequestHeaders(MacOSNativeWebResourceRequestedEventArgs owner) => _owner = owner;

	public void SetHeader(string name, string value)
	{
		_headers[name] = value;
		_owner.TrackHeaderChange(name, value);
	}

	public void RemoveHeader(string name)
	{
		_headers.Remove(name);
		_owner.TrackHeaderRemoval(name);
	}

	public string GetHeader(string name) => _headers.TryGetValue(name, out var v) ? v : string.Empty;
	public bool Contains(string name) => _headers.ContainsKey(name);
	public INativeHttpHeadersCollectionIterator GetHeaders(string name) => new MacOSEmptyIterator();
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _headers.GetEnumerator();
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

internal class MacOSEmptyIterator : INativeHttpHeadersCollectionIterator
{
	public object Current => null!;
	public bool HasCurrent => false;
	public bool MoveNext() => false;
	public uint GetMany(object items) => 0;
}
