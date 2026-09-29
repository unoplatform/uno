#nullable enable
using Android.Webkit;
using Java.Interop;

namespace Uno.UI.Xaml.Controls;

internal class UnoWebViewHandler : UnoWebViewHandlerJavascriptInterface
{
	private readonly NativeWebViewWrapper _nativeWebView;

	public UnoWebViewHandler(NativeWebViewWrapper wrapper)
	{
		_nativeWebView = wrapper;
	}

	[JavascriptInterface]
	public override void PostMessage(string? message) => _nativeWebView?.OnWebMessageReceived(message);
}
