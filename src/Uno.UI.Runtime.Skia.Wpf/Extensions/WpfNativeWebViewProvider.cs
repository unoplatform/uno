extern alias WpfWebView;

using Windows.UI.Xaml.Controls;
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
		if (content is null)
		{
			content = new WpfWebView.Microsoft.Web.WebView2.Wpf.WebView2();
			contentPresenter.Content = content;
		}

		return new WpfNativeWebView(content, _owner);
	}
}
