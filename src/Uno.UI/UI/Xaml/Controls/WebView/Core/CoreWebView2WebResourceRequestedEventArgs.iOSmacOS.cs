#if __IOS__ || __MACOS__
#nullable enable

using System;
using Foundation;
using Windows.Foundation;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// iOS/macOS implementation of WebResourceRequestedEventArgs.
/// 
/// WKWEBVIEW LIMITATIONS (READ CAREFULLY):
/// ========================================
/// 
/// 1. READ-ONLY HEADER ACCESS:
///    You can READ request headers (args.Request.Headers.GetHeader("name")),
///    but MODIFICATIONS will NOT be applied to the actual WKWebView request.
///    WKWebView does not support modifying headers after request initiation.
///    
/// 2. NO CUSTOM RESPONSES:
///    Setting args.Response will have NO EFFECT. WKNavigationDelegate cannot
///    provide custom response content.
///    
/// 3. NAVIGATION ACTIONS ONLY:
///    This event only fires for main document navigation, NOT for sub-resources
///    like images, scripts, or stylesheets.
///    
/// WHAT YOU CAN DO:
/// - Read request URL and headers
/// - Log or track requests
/// - Cancel navigation (by other means)
///    
/// WORKAROUNDS FOR FULL FUNCTIONALITY:
/// 1. Use WKURLSchemeHandler for custom URL schemes
/// 2. Inject JavaScript to override fetch/XMLHttpRequest
/// 3. Use a proxy server to inject headers
/// </summary>
public partial class CoreWebView2WebResourceRequestedEventArgs
{
	private CoreWebView2WebResourceRequest? _request;
	private CoreWebView2WebResourceResponse? _response;
	private readonly CoreWebView2WebResourceContext _resourceContext;
	private readonly NSUrlRequest? _nativeRequest;
	private Deferral? _deferral;

	internal CoreWebView2WebResourceRequestedEventArgs(
		NSUrlRequest? nativeRequest,
		CoreWebView2WebResourceContext resourceContext)
	{
		_nativeRequest = nativeRequest;
		_resourceContext = resourceContext;
	}

	public CoreWebView2WebResourceRequest Request
		=> _request ??= new CoreWebView2WebResourceRequest(_nativeRequest);

	/// <summary>
	/// Gets or sets the response. 
	/// WARNING: This property exists for API compatibility but has NO EFFECT on iOS/macOS.
	/// WKWebView cannot provide custom responses.
	/// </summary>
	public CoreWebView2WebResourceResponse Response
	{
		get => _response ??= new CoreWebView2WebResourceResponse();
		set => _response = value;
	}

	public CoreWebView2WebResourceContext ResourceContext => _resourceContext;

	public CoreWebView2WebResourceRequestSourceKinds RequestedSourceKind
		=> CoreWebView2WebResourceRequestSourceKinds.Document;

	/// <summary>
	/// Indicates whether headers were modified.
	/// NOTE: Modifications are tracked but cannot be applied on iOS/macOS.
	/// </summary>
	internal bool HasHeaderModifications => _request?.HasModifiedHeaders ?? false;

	public Deferral GetDeferral()
	{
		_deferral = new Deferral(() => { });
		return _deferral;
	}
}
#endif
