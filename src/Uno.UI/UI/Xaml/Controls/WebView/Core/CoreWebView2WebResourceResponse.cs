#nullable enable

using System;
#if __SKIA__
using Windows.Storage.Streams;
#endif

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Represents a custom response for the WebResourceRequested event.
/// </summary>
public partial class CoreWebView2WebResourceResponse
{
#if __SKIA__
	private readonly INativeWebResourceResponse _nativeResponse;
	private CoreWebView2HttpResponseHeaders? _headers;

	internal CoreWebView2WebResourceResponse(object nativeResponse)
	{
		if (nativeResponse is INativeWebResourceResponse wrapper)
		{
			_nativeResponse = wrapper;
		}
		else
		{
			_nativeResponse = new ReflectionNativeWebResourceResponse(nativeResponse ?? throw new ArgumentNullException(nameof(nativeResponse)));
		}
	}

	internal object NativeResponse => _nativeResponse is ReflectionNativeWebResourceResponse r ? r.Target : _nativeResponse;
	internal INativeWebResourceResponse NativeResponseInterface => _nativeResponse;

	public IRandomAccessStream Content
	{
		get => _nativeResponse.Content;
		set => _nativeResponse.Content = value;
	}

	public CoreWebView2HttpResponseHeaders Headers
		=> _headers ??= new CoreWebView2HttpResponseHeaders(_nativeResponse.Headers);

	public int StatusCode
	{
		get => _nativeResponse.StatusCode;
		set => _nativeResponse.StatusCode = value;
	}

	public string ReasonPhrase
	{
		get => _nativeResponse.ReasonPhrase;
		set => _nativeResponse.ReasonPhrase = value;
	}
#endif
}
