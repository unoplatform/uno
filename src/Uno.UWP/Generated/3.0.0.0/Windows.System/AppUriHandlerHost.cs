#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppUriHandlerHost 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AppUriHandlerHost.Name is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20AppUriHandlerHost.Name");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.AppUriHandlerHost", "string AppUriHandlerHost.Name");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool AppUriHandlerHost.IsEnabled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20AppUriHandlerHost.IsEnabled");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.AppUriHandlerHost", "bool AppUriHandlerHost.IsEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AppUriHandlerHost( string name) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.AppUriHandlerHost", "AppUriHandlerHost.AppUriHandlerHost(string name)");
		}
		#endif
		// Forced skipping of method Windows.System.AppUriHandlerHost.AppUriHandlerHost(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AppUriHandlerHost() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.AppUriHandlerHost", "AppUriHandlerHost.AppUriHandlerHost()");
		}
		#endif
		// Forced skipping of method Windows.System.AppUriHandlerHost.AppUriHandlerHost()
		// Forced skipping of method Windows.System.AppUriHandlerHost.Name.get
		// Forced skipping of method Windows.System.AppUriHandlerHost.Name.set
		// Forced skipping of method Windows.System.AppUriHandlerHost.IsEnabled.get
		// Forced skipping of method Windows.System.AppUriHandlerHost.IsEnabled.set
	}
}
