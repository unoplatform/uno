using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

internal sealed class UIKitNativeWebViewProvider : INativeWebViewProvider
{
	private readonly CoreWebView2 _owner;

	public UIKitNativeWebViewProvider(CoreWebView2 owner)
	{
		_owner = owner;
	}

	public INativeWebView CreateNativeWebView(ContentPresenter contentPresenter)
	{
		var content = contentPresenter.Content as INativeWebView;
		if (content is null)
		{
			content = new NativeWebView();
			contentPresenter.Content = content;
		}

		content.SetOwner(_owner);
		return content;
	}
}
