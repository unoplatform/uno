#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.DialProtocol
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DialApp 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AppName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DialApp.AppName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20DialApp.AppName");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.DialProtocol.DialApp.AppName.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.DialProtocol.DialAppLaunchResult> RequestLaunchAsync( string appArgument)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DialAppLaunchResult> DialApp.RequestLaunchAsync(string appArgument) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CDialAppLaunchResult%3E%20DialApp.RequestLaunchAsync%28string%20appArgument%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.DialProtocol.DialAppStopResult> StopAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DialAppStopResult> DialApp.StopAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CDialAppStopResult%3E%20DialApp.StopAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.DialProtocol.DialAppStateDetails> GetAppStateAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DialAppStateDetails> DialApp.GetAppStateAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CDialAppStateDetails%3E%20DialApp.GetAppStateAsync%28%29");
		}
		#endif
	}
}
