#if __WASM__
#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// WASM implementation of WebResourceRequest.
/// </summary>
public partial class CoreWebView2WebResourceRequest
{
	private CoreWebView2HttpRequestHeaders? _headers;
	private string _uri;
	private string _method;

	internal CoreWebView2WebResourceRequest(string uri, string method, IDictionary<string, string>? headers = null)
	{
		_uri = uri;
		_method = method;
		_headers = new CoreWebView2HttpRequestHeaders(headers);
	}

	public string Uri
	{
		get => _uri;
		set => _uri = value;
	}

	public string Method
	{
		get => _method;
		set => _method = value;
	}

	public global::Windows.Storage.Streams.IRandomAccessStream Content
	{
		get => throw new NotSupportedException("Content stream is not available on WASM WebResourceRequest.");
		set => throw new NotSupportedException("Setting Content is not supported on WASM WebResourceRequest.");
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
