#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreWebView2Environment 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string BrowserVersionString
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2Environment.BrowserVersionString is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2Environment.BrowserVersionString");
			}
		}
		#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2Environment.BrowserVersionString.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2Environment.NewBrowserVersionAvailable.add
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2Environment.NewBrowserVersionAvailable.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Microsoft.Web.WebView2.Core.CoreWebView2Controller> CreateCoreWebView2ControllerAsync( global::Microsoft.Web.WebView2.Core.CoreWebView2ControllerWindowReference ParentWindow)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CoreWebView2Controller> CoreWebView2Environment.CreateCoreWebView2ControllerAsync(CoreWebView2ControllerWindowReference ParentWindow) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CCoreWebView2Controller%3E%20CoreWebView2Environment.CreateCoreWebView2ControllerAsync%28CoreWebView2ControllerWindowReference%20ParentWindow%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2WebResourceResponse CreateWebResourceResponse( global::Windows.Storage.Streams.IRandomAccessStream Content,  int StatusCode,  string ReasonPhrase,  string Headers)
		{
			throw new global::System.NotImplementedException("The member CoreWebView2WebResourceResponse CoreWebView2Environment.CreateWebResourceResponse(IRandomAccessStream Content, int StatusCode, string ReasonPhrase, string Headers) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2WebResourceResponse%20CoreWebView2Environment.CreateWebResourceResponse%28IRandomAccessStream%20Content%2C%20int%20StatusCode%2C%20string%20ReasonPhrase%2C%20string%20Headers%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequest CreateWebResourceRequest( string uri,  string Method,  global::Windows.Storage.Streams.IRandomAccessStream postData,  string Headers)
		{
			throw new global::System.NotImplementedException("The member CoreWebView2WebResourceRequest CoreWebView2Environment.CreateWebResourceRequest(string uri, string Method, IRandomAccessStream postData, string Headers) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2WebResourceRequest%20CoreWebView2Environment.CreateWebResourceRequest%28string%20uri%2C%20string%20Method%2C%20IRandomAccessStream%20postData%2C%20string%20Headers%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Microsoft.Web.WebView2.Core.CoreWebView2CompositionController> CreateCoreWebView2CompositionControllerAsync( global::Microsoft.Web.WebView2.Core.CoreWebView2ControllerWindowReference ParentWindow)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CoreWebView2CompositionController> CoreWebView2Environment.CreateCoreWebView2CompositionControllerAsync(CoreWebView2ControllerWindowReference ParentWindow) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CCoreWebView2CompositionController%3E%20CoreWebView2Environment.CreateCoreWebView2CompositionControllerAsync%28CoreWebView2ControllerWindowReference%20ParentWindow%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.Web.WebView2.Core.CoreWebView2PointerInfo CreateCoreWebView2PointerInfo()
		{
			throw new global::System.NotImplementedException("The member CoreWebView2PointerInfo CoreWebView2Environment.CreateCoreWebView2PointerInfo() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CoreWebView2PointerInfo%20CoreWebView2Environment.CreateCoreWebView2PointerInfo%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Microsoft.Web.WebView2.Core.CoreWebView2Environment> CreateAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CoreWebView2Environment> CoreWebView2Environment.CreateAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CCoreWebView2Environment%3E%20CoreWebView2Environment.CreateAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Microsoft.Web.WebView2.Core.CoreWebView2Environment> CreateWithOptionsAsync( string browserExecutableFolder,  string userDataFolder,  global::Microsoft.Web.WebView2.Core.CoreWebView2EnvironmentOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CoreWebView2Environment> CoreWebView2Environment.CreateWithOptionsAsync(string browserExecutableFolder, string userDataFolder, CoreWebView2EnvironmentOptions options) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CCoreWebView2Environment%3E%20CoreWebView2Environment.CreateWithOptionsAsync%28string%20browserExecutableFolder%2C%20string%20userDataFolder%2C%20CoreWebView2EnvironmentOptions%20options%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetAvailableBrowserVersionString()
		{
			throw new global::System.NotImplementedException("The member string CoreWebView2Environment.GetAvailableBrowserVersionString() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2Environment.GetAvailableBrowserVersionString%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetAvailableBrowserVersionString( string browserExecutableFolder)
		{
			throw new global::System.NotImplementedException("The member string CoreWebView2Environment.GetAvailableBrowserVersionString(string browserExecutableFolder) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20CoreWebView2Environment.GetAvailableBrowserVersionString%28string%20browserExecutableFolder%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static int CompareBrowserVersionString( string browserVersionString1,  string browserVersionString2)
		{
			throw new global::System.NotImplementedException("The member int CoreWebView2Environment.CompareBrowserVersionString(string browserVersionString1, string browserVersionString2) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20CoreWebView2Environment.CompareBrowserVersionString%28string%20browserVersionString1%2C%20string%20browserVersionString2%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Microsoft.Web.WebView2.Core.CoreWebView2Environment, object> NewBrowserVersionAvailable
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2Environment", "event TypedEventHandler<CoreWebView2Environment, object> CoreWebView2Environment.NewBrowserVersionAvailable");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2Environment", "event TypedEventHandler<CoreWebView2Environment, object> CoreWebView2Environment.NewBrowserVersionAvailable");
			}
		}
		#endif
	}
}
