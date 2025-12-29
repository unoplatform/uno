#nullable enable

using System;
#if __ANDROID__ || __IOS__ || __MACOS__ || __WASM__ || ANDROID_SKIA || UIKIT_SKIA
using System.Collections.Generic;
#endif
#if __ANDROID__ || ANDROID_SKIA
using Android.Webkit;
#elif __IOS__ || __MACOS__ || UIKIT_SKIA
using Foundation;
#elif __SKIA__
using Windows.Storage.Streams;
#endif

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Represents a web resource request for the WebResourceRequested event.
/// </summary>
public partial class CoreWebView2WebResourceRequest
{
#if __SKIA__
	private readonly dynamic _nativeRequest;
	private CoreWebView2HttpRequestHeaders? _headers;

	internal CoreWebView2WebResourceRequest(object nativeRequest)
	{
		_nativeRequest = nativeRequest ?? throw new ArgumentNullException(nameof(nativeRequest));
	}

	internal dynamic NativeRequest => _nativeRequest;

	public string Uri
	{
		get => _nativeRequest.Uri;
		set => _nativeRequest.Uri = value;
	}

	public string Method
	{
		get => _nativeRequest.Method;
		set => _nativeRequest.Method = value;
	}

	public IRandomAccessStream Content
	{
		get => _nativeRequest.Content;
		set => _nativeRequest.Content = value;
	}

	public CoreWebView2HttpRequestHeaders Headers
		=> _headers ??= new CoreWebView2HttpRequestHeaders(_nativeRequest.Headers);
#elif __ANDROID__ || __IOS__ || __MACOS__ || __WASM__ || ANDROID_SKIA || UIKIT_SKIA
	private CoreWebView2HttpRequestHeaders? _headers;
	private string _uri;
	private string _method;

#if __ANDROID__ || ANDROID_SKIA
	private readonly IWebResourceRequest? _nativeRequest;

	internal CoreWebView2WebResourceRequest(IWebResourceRequest? nativeRequest)
	{
		_nativeRequest = nativeRequest;
		_uri = nativeRequest?.Url?.ToString() ?? string.Empty;
		_method = nativeRequest?.Method ?? "GET";

		var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (nativeRequest?.RequestHeaders != null)
		{
			foreach (var key in nativeRequest.RequestHeaders.Keys)
			{
				if (key != null && nativeRequest.RequestHeaders.TryGetValue(key, out var value) && value != null)
				{
					headers[key] = value;
				}
			}
		}

		_headers = new CoreWebView2HttpRequestHeaders(headers);
	}
#elif __IOS__ || __MACOS__ || UIKIT_SKIA
	private readonly NSUrlRequest? _nativeRequest;

	internal CoreWebView2WebResourceRequest(NSUrlRequest? nativeRequest)
	{
		_nativeRequest = nativeRequest;
		_uri = nativeRequest?.Url?.AbsoluteString ?? string.Empty;
		_method = nativeRequest?.HttpMethod ?? "GET";
		_headers = new CoreWebView2HttpRequestHeaders(nativeRequest?.Headers);
	}
#elif __WASM__
	internal CoreWebView2WebResourceRequest(string uri, string method, IDictionary<string, string>? headers = null)
	{
		_uri = uri;
		_method = method;
		_headers = new CoreWebView2HttpRequestHeaders(headers);
	}
#endif

	public string Uri
	{
		get => _uri;
		set => _uri = value ?? string.Empty;
	}

	public string Method
	{
		get => _method;
		set => _method = string.IsNullOrWhiteSpace(value) ? _method : value;
	}

	public global::Windows.Storage.Streams.IRandomAccessStream Content
	{
#if __ANDROID__ || ANDROID_SKIA
		get => throw new NotSupportedException("Content stream is not available on Android WebResourceRequest.");
		set => throw new NotSupportedException("Setting Content is not supported on Android WebResourceRequest.");
#elif __IOS__ || __MACOS__ || UIKIT_SKIA
		get => throw new NotSupportedException("Content stream is not available on iOS/macOS WebResourceRequest.");
		set => throw new NotSupportedException("Setting Content is not supported on iOS/macOS WebResourceRequest.");
#elif __WASM__
		get => throw new NotSupportedException("Content stream is not available on WASM WebResourceRequest.");
		set => throw new NotSupportedException("Setting Content is not supported on WASM WebResourceRequest.");
#endif
	}

	public CoreWebView2HttpRequestHeaders Headers
		=> _headers ??= new CoreWebView2HttpRequestHeaders(
#if __ANDROID__ || __WASM__ || ANDROID_SKIA
			null
#elif __IOS__ || __MACOS__ || UIKIT_SKIA
			(NSDictionary?)null
#endif
		);

	/// <summary>
	/// Indicates whether headers have been modified.
	/// </summary>
	internal bool HasModifiedHeaders => _headers?.HasModifications ?? false;

	/// <summary>
	/// Gets the effective headers after modifications.
	/// </summary>
	internal Dictionary<string, string>? GetEffectiveHeaders()
		=> _headers?.GetEffectiveHeaders();
#endif
}
