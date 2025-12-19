#if __IOS__ || __MACOS__
#nullable enable

using System;
using Foundation;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// iOS/macOS implementation of WebResourceRequest wrapping NSUrlRequest.
/// </summary>
public partial class CoreWebView2WebResourceRequest
{
	private CoreWebView2HttpRequestHeaders? _headers;
	private readonly NSUrlRequest? _nativeRequest;
	private string _uri;
	private string _method;

	internal CoreWebView2WebResourceRequest(NSUrlRequest? nativeRequest)
	{
		_nativeRequest = nativeRequest;
		_uri = nativeRequest?.Url?.AbsoluteString ?? string.Empty;
		_method = nativeRequest?.HttpMethod ?? "GET";
		_headers = new CoreWebView2HttpRequestHeaders(nativeRequest?.Headers);
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
		get => throw new NotSupportedException("Content stream is not available on iOS/macOS WebResourceRequest.");
		set => throw new NotSupportedException("Setting Content is not supported on iOS/macOS WebResourceRequest.");
	}

	public CoreWebView2HttpRequestHeaders Headers
		=> _headers ??= new CoreWebView2HttpRequestHeaders();

	/// <summary>
	/// Indicates whether headers have been modified.
	/// NOTE: On iOS/macOS, modifications are tracked but cannot be applied.
	/// </summary>
	internal bool HasModifiedHeaders => _headers?.HasModifications ?? false;
}
#endif
