#if __ANDROID__ || __UNO_SKIA_ANDROID__
#nullable enable

using Android.Webkit;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// WebViewClient that intercepts web resource requests and allows header modification.
/// </summary>
internal class WebResourceInterceptingClient : InternalClient
{
	private readonly NativeWebViewWrapper _owner;

	public WebResourceInterceptingClient(CoreWebView2 coreWebView, NativeWebViewWrapper owner)
		: base(coreWebView, owner)
	{
		_owner = owner;
	}

	public override WebResourceResponse? ShouldInterceptRequest(WebView? view, IWebResourceRequest? request)
	{
		var response = _owner.OnWebResourceRequested(request);
		return response ?? base.ShouldInterceptRequest(view, request);
	}
}
#endif
