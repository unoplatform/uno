#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VpnDomainNameAssignment 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri ProxyAutoConfigurationUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri VpnDomainNameAssignment.ProxyAutoConfigurationUri is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnDomainNameAssignment", "Uri VpnDomainNameAssignment.ProxyAutoConfigurationUri");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Networking.Vpn.VpnDomainNameInfo> DomainNameList
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<VpnDomainNameInfo> VpnDomainNameAssignment.DomainNameList is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public VpnDomainNameAssignment() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnDomainNameAssignment", "VpnDomainNameAssignment.VpnDomainNameAssignment()");
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.VpnDomainNameAssignment.VpnDomainNameAssignment()
		// Forced skipping of method Windows.Networking.Vpn.VpnDomainNameAssignment.DomainNameList.get
		// Forced skipping of method Windows.Networking.Vpn.VpnDomainNameAssignment.ProxyAutoConfigurationUri.set
		// Forced skipping of method Windows.Networking.Vpn.VpnDomainNameAssignment.ProxyAutoConfigurationUri.get
	}
}
