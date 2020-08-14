#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebSocketKeepAlive : global::Windows.ApplicationModel.Background.IBackgroundTask
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WebSocketKeepAlive() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.WebSocketKeepAlive", "WebSocketKeepAlive.WebSocketKeepAlive()");
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.WebSocketKeepAlive.WebSocketKeepAlive()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Run( global::Windows.ApplicationModel.Background.IBackgroundTaskInstance taskInstance)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.WebSocketKeepAlive", "void WebSocketKeepAlive.Run(IBackgroundTaskInstance taskInstance)");
		}
		#endif
		// Processing: Windows.ApplicationModel.Background.IBackgroundTask
	}
}
