#if __ANDROID__
#nullable enable

using System;
using System.Collections.Generic;
using Android.Webkit;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Android implementation of WebResourceRequest wrapping the native Android IWebResourceRequest.
/// </summary>
public partial class CoreWebView2WebResourceRequest
{
	private CoreWebView2HttpRequestHeaders? _headers;
	private readonly IWebResourceRequest? _nativeRequest;
	private string _uri;
	private string _method;

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
		get => throw new NotSupportedException("Content stream is not available on Android WebResourceRequest.");
		set => throw new NotSupportedException("Setting Content is not supported on Android WebResourceRequest.");
	}

	public CoreWebView2HttpRequestHeaders Headers
		=> _headers ??= new CoreWebView2HttpRequestHeaders();

	/// <summary>
	/// Indicates whether headers have been modified.
	/// </summary>
	internal bool HasModifiedHeaders => _headers?.HasModifications ?? false;

	/// <summary>
	/// Gets the effective headers after modifications.
	/// </summary>
	internal Dictionary<string, string>? GetEffectiveHeaders()
		=> _headers?.GetEffectiveHeaders();
}
#endif
