using Android.Webkit;
using Java.Interop;

namespace Uno.UI.Xaml.Controls;

internal class UnoWebMessageHandler : Java.Lang.Object
{
	private readonly NativeWebViewWrapper _nativeWebView;

	public UnoWebMessageHandler(NativeWebViewWrapper wrapper)
	{
		_nativeWebView = wrapper;
	}

	[Export]
	[JavascriptInterface]
	public void postMessage(string message) => _nativeWebView?.OnWebMessageReceived(message);
}
