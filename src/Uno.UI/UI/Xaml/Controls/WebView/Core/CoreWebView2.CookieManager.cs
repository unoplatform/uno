#nullable enable

using Uno.UI.Xaml.Controls;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// CoreWebView2 partial implementation for CookieManager property
/// </summary>
public partial class CoreWebView2
{
	private CoreWebView2CookieManager? _cookieManager;

#if __ANDROID__ || __IOS__ || __TVOS__ || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__
	/// <summary>
	/// Gets the CoreWebView2CookieManager object associated with this CoreWebView2.
	/// </summary>
	public CoreWebView2CookieManager CookieManager
	{
		get
		{
			if (_cookieManager == null && _nativeWebView != null)
			{
				_cookieManager = new CoreWebView2CookieManager(_nativeWebView);
			}

			if (_cookieManager == null)
			{
				throw new InvalidOperationException("CookieManager is not available. Ensure the WebView is initialized.");
			}

			return _cookieManager;
		}
	}
#endif
}
