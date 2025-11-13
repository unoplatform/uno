using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Extensibility;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSNativeWebViewProvider : INativeWebViewProvider
{
	private readonly CoreWebView2 _owner;

	public static unsafe void Register()
	{
		NativeUno.uno_set_execute_callback(&MacOSNativeWebView.ExecuteScriptCallback);
		NativeUno.uno_set_invoke_callback(&MacOSNativeWebView.InvokeScriptCallback);
		NativeUno.uno_set_webview_unsupported_scheme_identified_callback(&MacOSNativeWebView.OnUnsupportedUriSchemeIdentified);
		NativeUno.uno_set_webview_navigation_callbacks(
			&MacOSNativeWebView.NavigationStartingCallback,
			&MacOSNativeWebView.NavigationFinishingCallback,
			&MacOSNativeWebView.DidChangeValue,
			&MacOSNativeWebView.DidReceiveScriptMessage,
			&MacOSNativeWebView.NavigationFailingCallback);

		NativeUno.uno_set_webview_new_window_requested_callback(&MacOSNativeWebView.NewWindowRequestedCallback);

		ApiExtensibility.Register<CoreWebView2>(typeof(INativeWebViewProvider), o => new MacOSNativeWebViewProvider(o));
	}

	private MacOSNativeWebViewProvider(CoreWebView2 owner)
	{
		_owner = owner;
	}

	public INativeWebView CreateNativeWebView(ContentPresenter contentPresenter)
	{
		var window = contentPresenter.XamlRoot?.HostWindow?.NativeWindow as MacOSWindowNative;
		if (contentPresenter.Content is not MacOSNativeWebView content)
		{
			content = new MacOSNativeWebView(window!, _owner);
			contentPresenter.Content = content;
		}
		return content;
	}
}
