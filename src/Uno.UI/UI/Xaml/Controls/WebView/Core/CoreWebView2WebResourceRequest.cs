#nullable enable

using System;
#if __SKIA__
using Windows.Storage.Streams;
#endif

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Represents a web resource request for the WebResourceRequested event.
/// </summary>
public partial class CoreWebView2WebResourceRequest
{
#if __SKIA__
	private readonly INativeWebResourceRequest _nativeRequest;
	private CoreWebView2HttpRequestHeaders? _headers;

	internal CoreWebView2WebResourceRequest(object nativeRequest)
	{
		if (nativeRequest is INativeWebResourceRequest wrapper)
		{
			_nativeRequest = wrapper;
		}
		else
		{
			_nativeRequest = new ReflectionNativeWebResourceRequest(nativeRequest ?? throw new ArgumentNullException(nameof(nativeRequest)));
		}
	}

	internal object NativeRequest => _nativeRequest is ReflectionNativeWebResourceRequest r ? r.Target : _nativeRequest;

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
#endif
}
