#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VpnAppId 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VpnAppId.Value is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnAppId", "string VpnAppId.Value");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Vpn.VpnAppIdType Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member VpnAppIdType VpnAppId.Type is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnAppId", "VpnAppIdType VpnAppId.Type");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public VpnAppId( global::Windows.Networking.Vpn.VpnAppIdType type,  string value) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnAppId", "VpnAppId.VpnAppId(VpnAppIdType type, string value)");
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.VpnAppId.VpnAppId(Windows.Networking.Vpn.VpnAppIdType, string)
		// Forced skipping of method Windows.Networking.Vpn.VpnAppId.Type.get
		// Forced skipping of method Windows.Networking.Vpn.VpnAppId.Type.set
		// Forced skipping of method Windows.Networking.Vpn.VpnAppId.Value.get
		// Forced skipping of method Windows.Networking.Vpn.VpnAppId.Value.set
	}
}
