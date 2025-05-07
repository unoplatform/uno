using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidNativeWebViewProvider : INativeWebViewProvider
{
	private readonly CoreWebView2 _owner;

	public AndroidNativeWebViewProvider(CoreWebView2 owner)
	{
		_owner = owner;
	}

	public INativeWebView CreateNativeWebView(ContentPresenter contentPresenter)
	{
		var content = contentPresenter.Content as global::Android.Webkit.WebView;
		if (content is null)
		{
			content = new global::Android.Webkit.WebView(ContextHelper.Current);
			contentPresenter.Content = content;
		}

		return new NativeWebViewWrapper(content, _owner);
	}
}
