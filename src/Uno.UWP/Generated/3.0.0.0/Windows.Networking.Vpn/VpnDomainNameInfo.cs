#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VpnDomainNameInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Vpn.VpnDomainNameType DomainNameType
		{
			get
			{
				throw new global::System.NotImplementedException("The member VpnDomainNameType VpnDomainNameInfo.DomainNameType is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnDomainNameInfo", "VpnDomainNameType VpnDomainNameInfo.DomainNameType");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.HostName DomainName
		{
			get
			{
				throw new global::System.NotImplementedException("The member HostName VpnDomainNameInfo.DomainName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnDomainNameInfo", "HostName VpnDomainNameInfo.DomainName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Networking.HostName> DnsServers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<HostName> VpnDomainNameInfo.DnsServers is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Networking.HostName> WebProxyServers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<HostName> VpnDomainNameInfo.WebProxyServers is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::System.Uri> WebProxyUris
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<Uri> VpnDomainNameInfo.WebProxyUris is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public VpnDomainNameInfo( string name,  global::Windows.Networking.Vpn.VpnDomainNameType nameType,  global::System.Collections.Generic.IEnumerable<global::Windows.Networking.HostName> dnsServerList,  global::System.Collections.Generic.IEnumerable<global::Windows.Networking.HostName> proxyServerList) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnDomainNameInfo", "VpnDomainNameInfo.VpnDomainNameInfo(string name, VpnDomainNameType nameType, IEnumerable<HostName> dnsServerList, IEnumerable<HostName> proxyServerList)");
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.VpnDomainNameInfo.VpnDomainNameInfo(string, Windows.Networking.Vpn.VpnDomainNameType, System.Collections.Generic.IEnumerable<Windows.Networking.HostName>, System.Collections.Generic.IEnumerable<Windows.Networking.HostName>)
		// Forced skipping of method Windows.Networking.Vpn.VpnDomainNameInfo.DomainName.set
		// Forced skipping of method Windows.Networking.Vpn.VpnDomainNameInfo.DomainName.get
		// Forced skipping of method Windows.Networking.Vpn.VpnDomainNameInfo.DomainNameType.set
		// Forced skipping of method Windows.Networking.Vpn.VpnDomainNameInfo.DomainNameType.get
		// Forced skipping of method Windows.Networking.Vpn.VpnDomainNameInfo.DnsServers.get
		// Forced skipping of method Windows.Networking.Vpn.VpnDomainNameInfo.WebProxyServers.get
		// Forced skipping of method Windows.Networking.Vpn.VpnDomainNameInfo.WebProxyUris.get
	}
}
