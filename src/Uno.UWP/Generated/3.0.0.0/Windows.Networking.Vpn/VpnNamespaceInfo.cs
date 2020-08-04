#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VpnNamespaceInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Networking.HostName> WebProxyServers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<HostName> VpnNamespaceInfo.WebProxyServers is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnNamespaceInfo", "IList<HostName> VpnNamespaceInfo.WebProxyServers");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Namespace
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VpnNamespaceInfo.Namespace is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnNamespaceInfo", "string VpnNamespaceInfo.Namespace");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Networking.HostName> DnsServers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<HostName> VpnNamespaceInfo.DnsServers is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnNamespaceInfo", "IList<HostName> VpnNamespaceInfo.DnsServers");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public VpnNamespaceInfo( string name,  global::System.Collections.Generic.IList<global::Windows.Networking.HostName> dnsServerList,  global::System.Collections.Generic.IList<global::Windows.Networking.HostName> proxyServerList) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnNamespaceInfo", "VpnNamespaceInfo.VpnNamespaceInfo(string name, IList<HostName> dnsServerList, IList<HostName> proxyServerList)");
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.VpnNamespaceInfo.VpnNamespaceInfo(string, System.Collections.Generic.IList<Windows.Networking.HostName>, System.Collections.Generic.IList<Windows.Networking.HostName>)
		// Forced skipping of method Windows.Networking.Vpn.VpnNamespaceInfo.Namespace.set
		// Forced skipping of method Windows.Networking.Vpn.VpnNamespaceInfo.Namespace.get
		// Forced skipping of method Windows.Networking.Vpn.VpnNamespaceInfo.DnsServers.set
		// Forced skipping of method Windows.Networking.Vpn.VpnNamespaceInfo.DnsServers.get
		// Forced skipping of method Windows.Networking.Vpn.VpnNamespaceInfo.WebProxyServers.set
		// Forced skipping of method Windows.Networking.Vpn.VpnNamespaceInfo.WebProxyServers.get
	}
}
