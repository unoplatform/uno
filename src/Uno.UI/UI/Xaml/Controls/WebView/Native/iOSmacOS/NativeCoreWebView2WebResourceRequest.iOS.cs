#nullable enable

using System;
using System.Collections.Generic;
using Foundation;
using Microsoft.Web.WebView2.Core;

namespace Uno.Web.WebView2.Core;

/// <summary>
/// iOS/macOS-specific implementation for WebResourceRequest.
/// </summary>
internal partial class NativeCoreWebView2WebResourceRequest : INativeWebResourceRequest
{
	private NativeCoreWebView2HttpRequestHeaders? _headers;
	private string _uri;
	private string _method;
	private readonly NSUrlRequest? _nativeRequest;

	internal NativeCoreWebView2WebResourceRequest(NSUrlRequest? nativeRequest)
	{
		_nativeRequest = nativeRequest;
		_uri = nativeRequest?.Url?.AbsoluteString ?? string.Empty;
		_method = nativeRequest?.HttpMethod ?? "GET";
		_headers = new NativeCoreWebView2HttpRequestHeaders(nativeRequest?.Headers);
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
		get => throw new NotSupportedException("Content stream is not available on iOS/macOS WebResourceRequest.");
		set => throw new NotSupportedException("Setting Content is not supported on iOS/macOS WebResourceRequest.");
	}

	public INativeHttpRequestHeaders Headers
		=> _headers ??= new NativeCoreWebView2HttpRequestHeaders((NSDictionary?)null);

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
