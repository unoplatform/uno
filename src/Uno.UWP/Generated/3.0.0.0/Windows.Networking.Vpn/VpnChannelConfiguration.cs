#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VpnChannelConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string CustomField
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VpnChannelConfiguration.CustomField is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.HostName> ServerHostNameList
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<HostName> VpnChannelConfiguration.ServerHostNameList is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ServerServiceName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VpnChannelConfiguration.ServerServiceName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::System.Uri> ServerUris
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<Uri> VpnChannelConfiguration.ServerUris is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.VpnChannelConfiguration.ServerServiceName.get
		// Forced skipping of method Windows.Networking.Vpn.VpnChannelConfiguration.ServerHostNameList.get
		// Forced skipping of method Windows.Networking.Vpn.VpnChannelConfiguration.CustomField.get
		// Forced skipping of method Windows.Networking.Vpn.VpnChannelConfiguration.ServerUris.get
	}
}
