#if __WASM__ || WASM_SKIA
#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Web.WebView2.Core;

namespace Uno.Web.WebView2.Core;

/// <summary>
/// WASM-specific implementation for WebResourceRequest.
/// </summary>
internal partial class NativeCoreWebView2WebResourceRequest : INativeWebResourceRequest
{
	private NativeCoreWebView2HttpRequestHeaders? _headers;
	private string _uri;
	private string _method;

	internal NativeCoreWebView2WebResourceRequest(string url, string method, IDictionary<string, string>? headers)
	{
		_uri = url ?? string.Empty;
		_method = string.IsNullOrWhiteSpace(method) ? "GET" : method;
		_headers = new NativeCoreWebView2HttpRequestHeaders(headers);
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
		get => throw new NotSupportedException("Content stream is not available on WASM WebResourceRequest.");
		set => throw new NotSupportedException("Setting Content is not supported on WASM WebResourceRequest.");
	}

	public INativeHttpRequestHeaders Headers
		=> _headers ??= new NativeCoreWebView2HttpRequestHeaders(null);

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
