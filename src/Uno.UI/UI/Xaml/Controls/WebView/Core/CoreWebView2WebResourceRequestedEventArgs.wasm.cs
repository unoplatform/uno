#if __WASM__
#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// WASM implementation of WebResourceRequestedEventArgs.
/// 
/// WASM LIMITATIONS:
/// =================
/// 1. ONLY fetch/XMLHttpRequest INTERCEPTED:
///    JavaScript fetch() and XMLHttpRequest calls can be intercepted via JS prototype patching.
///    Standard HTML elements (img src, script src, link href, etc.) CANNOT be intercepted.
///    
/// 2. IFRAME ISOLATION:
///    The WebView is implemented as an iframe.
///    Same-origin policy and CORS restrictions apply.
///    Cross-origin iframes cannot be controlled.
///    
/// 3. TIMING:
///    The JavaScript interceptor must be injected BEFORE content makes requests.
///    May miss requests made during initial page load.
///    
/// 4. NO CUSTOM RESPONSES:
///    Cannot provide custom response content.
/// </summary>
public partial class CoreWebView2WebResourceRequestedEventArgs
{
	private CoreWebView2WebResourceRequest? _request;
	private CoreWebView2WebResourceResponse? _response;
	private readonly CoreWebView2WebResourceContext _resourceContext;
	private readonly string _url;
	private readonly string _method;
	private readonly IDictionary<string, string>? _requestHeaders;
	private Deferral? _deferral;

	internal CoreWebView2WebResourceRequestedEventArgs(
		string url,
		string method,
		IDictionary<string, string>? headers,
		CoreWebView2WebResourceContext resourceContext)
	{
		_url = url;
		_method = method;
		_requestHeaders = headers;
		_resourceContext = resourceContext;
	}

	public CoreWebView2WebResourceRequest Request
		=> _request ??= new CoreWebView2WebResourceRequest(_url, _method, _requestHeaders);

	public CoreWebView2WebResourceResponse Response
	{
		get => _response ??= new CoreWebView2WebResourceResponse();
		set => _response = value;
	}

	public CoreWebView2WebResourceContext ResourceContext => _resourceContext;

	public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind
		=> CoreWebView2WebResourceRequestSourceKinds.All;

	/// <summary>
	/// Indicates whether headers have been modified.
	/// </summary>
	internal bool HasHeaderModifications => _request?.HasModifiedHeaders ?? false;

	/// <summary>
	/// Gets the effective headers after modifications.
	/// </summary>
	internal IDictionary<string, string>? GetEffectiveHeaders()
		=> _request?.GetEffectiveHeaders();

	public Deferral GetDeferral()
	{
		_deferral = new Deferral(() => { });
		return _deferral;
	}
}
#endif
