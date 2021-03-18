#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VpnRoute 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte PrefixSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte VpnRoute.PrefixSize is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnRoute", "byte VpnRoute.PrefixSize");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.HostName Address
		{
			get
			{
				throw new global::System.NotImplementedException("The member HostName VpnRoute.Address is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnRoute", "HostName VpnRoute.Address");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public VpnRoute( global::Windows.Networking.HostName address,  byte prefixSize) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnRoute", "VpnRoute.VpnRoute(HostName address, byte prefixSize)");
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.VpnRoute.VpnRoute(Windows.Networking.HostName, byte)
		// Forced skipping of method Windows.Networking.Vpn.VpnRoute.Address.set
		// Forced skipping of method Windows.Networking.Vpn.VpnRoute.Address.get
		// Forced skipping of method Windows.Networking.Vpn.VpnRoute.PrefixSize.set
		// Forced skipping of method Windows.Networking.Vpn.VpnRoute.PrefixSize.get
	}
}
