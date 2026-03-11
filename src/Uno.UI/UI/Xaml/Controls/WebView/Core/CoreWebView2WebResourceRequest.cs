#nullable enable

using System;
using Windows.Storage.Streams;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Represents a web resource request for the WebResourceRequested event.
/// </summary>
public partial class CoreWebView2WebResourceRequest
{
	private readonly INativeWebResourceRequest _nativeRequest;
	private CoreWebView2HttpRequestHeaders? _headers;

	internal CoreWebView2WebResourceRequest(INativeWebResourceRequest nativeRequest)
	{
		_nativeRequest = nativeRequest;
	}

	internal INativeWebResourceRequest NativeRequest => _nativeRequest;

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
}
