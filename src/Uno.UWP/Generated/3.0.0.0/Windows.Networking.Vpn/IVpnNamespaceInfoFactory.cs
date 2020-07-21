#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IVpnNamespaceInfoFactory 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Networking.Vpn.VpnNamespaceInfo CreateVpnNamespaceInfo( string name,  global::System.Collections.Generic.IList<global::Windows.Networking.HostName> dnsServerList,  global::System.Collections.Generic.IList<global::Windows.Networking.HostName> proxyServerList);
		#endif
	}
}
