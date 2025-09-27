using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Uno.UI.NativeElementHosting;
using Uno.UI.Runtime.Skia;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

internal class BrowserWebViewProvider : INativeWebViewProvider
{
	private readonly CoreWebView2 _owner;

	public BrowserWebViewProvider(CoreWebView2 owner)
	{
		_owner = owner;
	}

	public INativeWebView CreateNativeWebView(ContentPresenter contentPresenter)
	{
		var content = contentPresenter.Content as BrowserHtmlElement;
		if (content is null)
		{
			content = BrowserHtmlElement.CreateHtmlElement("iframe");
			contentPresenter.Content = content;
		}

		var nativeWebView = new NativeWebView(_owner, content.ElementId);
		return nativeWebView;
	}
}
