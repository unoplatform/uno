#if __ANDROID__
#nullable enable

using System;
using Android.Webkit;
using Windows.Foundation;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Android implementation of WebResourceRequestedEventArgs.
/// 
/// ANDROID LIMITATIONS:
/// ====================
/// 1. HEADER MODIFICATION REQUIRES RE-FETCH:
///    When headers are modified, Android's WebViewClient.shouldInterceptRequest()
///    requires us to make a new HTTP request with the modified headers using HttpClient.
///    This means the resource is fetched twice (once by WebView, once by HttpClient).
///    
/// 2. COOKIES/SESSION STATE:
///    The re-fetched request may not have the same cookies or session state as
///    the original WebView request, which can cause authentication issues.
///    
/// 3. POST REQUESTS:
///    POST requests with body content cannot be reliably re-fetched because
///    the request body is not accessible from IWebResourceRequest.
/// </summary>
public partial class CoreWebView2WebResourceRequestedEventArgs
{
	private CoreWebView2WebResourceRequest? _request;
	private CoreWebView2WebResourceResponse? _response;
	private readonly CoreWebView2WebResourceContext _resourceContext;
	private readonly IWebResourceRequest? _nativeRequest;
	private Deferral? _deferral;

	internal CoreWebView2WebResourceRequestedEventArgs(
		IWebResourceRequest? nativeRequest,
		CoreWebView2WebResourceContext resourceContext)
	{
		_nativeRequest = nativeRequest;
		_resourceContext = resourceContext;
	}

	public CoreWebView2WebResourceRequest Request
		=> _request ??= new CoreWebView2WebResourceRequest(_nativeRequest);

	public CoreWebView2WebResourceResponse Response
	{
		get => _response ??= new CoreWebView2WebResourceResponse();
		set => _response = value;
	}

	public CoreWebView2WebResourceContext ResourceContext => _resourceContext;

	public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind
		=> CoreWebView2WebResourceRequestSourceKinds.All;

	/// <summary>
	/// Indicates whether headers have been modified and require re-fetching.
	/// </summary>
	internal bool RequiresRefetch => _request?.HasModifiedHeaders ?? false;

	/// <summary>
	/// Gets the native Android WebResourceResponse if a custom response was set.
	/// </summary>
	internal WebResourceResponse? GetNativeResponse() => _response?.ToNativeResponse();

	/// <summary>
	/// Gets the effective headers for re-fetching if headers were modified.
	/// </summary>
	internal System.Collections.Generic.Dictionary<string, string>? GetEffectiveHeaders()
		=> _request?.GetEffectiveHeaders();

	public Deferral GetDeferral()
	{
		_deferral = new Deferral(() => { });
		return _deferral;
	}
}
#endif
