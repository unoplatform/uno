#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProxyConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanConnectDirectly
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ProxyConfiguration.CanConnectDirectly is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20ProxyConfiguration.CanConnectDirectly");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::System.Uri> ProxyUris
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<Uri> ProxyConfiguration.ProxyUris is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CUri%3E%20ProxyConfiguration.ProxyUris");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Connectivity.ProxyConfiguration.ProxyUris.get
		// Forced skipping of method Windows.Networking.Connectivity.ProxyConfiguration.CanConnectDirectly.get
	}
}
