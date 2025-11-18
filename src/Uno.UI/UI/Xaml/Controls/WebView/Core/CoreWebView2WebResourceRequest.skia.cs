#nullable enable

#if __SKIA__
using System;
using Windows.Storage.Streams;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2WebResourceRequest
{
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
}
#endif
