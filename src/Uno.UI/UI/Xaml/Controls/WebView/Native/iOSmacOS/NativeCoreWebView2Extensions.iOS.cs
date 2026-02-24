#if __IOS__ || UIKIT_SKIA
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Web.WebView2.Core;
using Uno.Web.WebView2.Core;

namespace Microsoft.Web.WebView2.Core;

internal static class NativeCoreWebView2Extensions
{
	public static NativeCoreWebView2WebResourceRequestedEventArgs ToUIKit(this CoreWebView2WebResourceRequestedEventArgs args)
		=> args.NativeArgs is NativeCoreWebView2WebResourceRequestedEventArgs nativeArgs
			? nativeArgs
			: throw new InvalidOperationException("The provided CoreWebView2WebResourceRequestedEventArgs is not backed by an iOS/macOS native implementation.");

	public static NativeCoreWebView2WebResourceResponse ToUIKit(this CoreWebView2WebResourceResponse args)
		=> args.NativeResponse is NativeCoreWebView2WebResourceResponse nativeResponse
			? nativeResponse
			: throw new InvalidOperationException("The provided CoreWebView2WebResourceResponse is not backed by an iOS/macOS native implementation.");

	public static NativeCoreWebView2WebResourceRequest ToUIKit(this CoreWebView2WebResourceRequest request)
		=> request.NativeRequest is NativeCoreWebView2WebResourceRequest nativeRequest
			? nativeRequest
			: throw new InvalidOperationException("The provided CoreWebView2WebResourceRequest is not backed by an iOS/macOS native implementation.");

	public static CoreWebView2HttpHeadersCollectionIterator GetHeadersIterator(this INativeHttpResponseHeaders headers, string name)
	{
		var obj = headers.GetHeaders(name);
		if (obj is INativeHttpHeadersCollectionIterator nativeIt)
		{
			return new CoreWebView2HttpHeadersCollectionIterator(nativeIt);
		}
		throw new InvalidOperationException("The provided headers collection is not backed by an iOS/macOS native iterator implementation.");
	}
}
#endif
