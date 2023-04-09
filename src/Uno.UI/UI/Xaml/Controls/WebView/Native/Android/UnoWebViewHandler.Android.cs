using Android.Webkit;
using Java.Interop;

namespace Uno.UI.Xaml.Controls;

internal class UnoWebViewHandler : Java.Lang.Object
{
	private readonly NativeWebViewWrapper _nativeWebView;

	public UnoWebViewHandler(NativeWebViewWrapper wrapper)
	{
		_nativeWebView = wrapper;
	}

	[Export]
	[JavascriptInterface]
	public void postMessage(string message) => _nativeWebView?.OnWebMessageReceived(message);
}
