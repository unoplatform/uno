using System;
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
			// Prefer the owning window's activity over the ambient foreground one.
			var context = (global::Android.Content.Context?)AndroidSkiaXamlRootHost.GetActivity(contentPresenter.XamlRoot)
				?? ContextHelper.Current
				?? throw new InvalidOperationException("No Android context is available to create the native WebView.");

			content = new global::Android.Webkit.WebView(context);
			contentPresenter.Content = content;
		}

		return new NativeWebViewWrapper(content, _owner);
	}
}
