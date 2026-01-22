#nullable enable

using System;
using Windows.Storage.Streams;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Represents a custom response for the WebResourceRequested event.
/// </summary>
public partial class CoreWebView2WebResourceResponse
{
	private readonly INativeWebResourceResponse _nativeResponse;
	private CoreWebView2HttpResponseHeaders? _headers;

	internal CoreWebView2WebResourceResponse(INativeWebResourceResponse nativeResponse)
	{
		_nativeResponse = nativeResponse;
	}

	internal object NativeResponse => _nativeResponse;

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
}
