#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class NetworkItem 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid NetworkId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid NetworkItem.NetworkId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Guid%20NetworkItem.NetworkId");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Connectivity.NetworkItem.NetworkId.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.NetworkTypes GetNetworkTypes()
		{
			throw new global::System.NotImplementedException("The member NetworkTypes NetworkItem.GetNetworkTypes() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=NetworkTypes%20NetworkItem.GetNetworkTypes%28%29");
		}
		#endif
	}
}
