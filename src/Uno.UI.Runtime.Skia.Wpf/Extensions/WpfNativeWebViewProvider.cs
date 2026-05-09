extern alias WpfWebView;

using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions;

internal sealed class WpfNativeWebViewProvider : INativeWebViewProvider
{
	private readonly CoreWebView2 _owner;

	public WpfNativeWebViewProvider(CoreWebView2 owner)
	{
		_owner = owner;
	}

	public INativeWebView CreateNativeWebView(ContentPresenter contentPresenter)
	{
		var content = contentPresenter.Content as WpfWebView.Microsoft.Web.WebView2.Wpf.WebView2;
		if (content is not null)
		{
			return new WpfNativeWebView(content, _owner);
		}

#if NET10_0_OR_GREATER
		var aotContent = contentPresenter.Content as NativeAotWebViewHost;
		if (aotContent is null)
		{
			aotContent = new NativeAotWebViewHost();
			contentPresenter.Content = aotContent;
		}

		return new NativeAotWebView(aotContent, _owner);
#else
		content = new WpfWebView.Microsoft.Web.WebView2.Wpf.WebView2();
		contentPresenter.Content = content;
		return new WpfNativeWebView(content, _owner);
#endif
	}
}
