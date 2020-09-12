#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VpnNamespaceAssignment 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri ProxyAutoConfigUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri VpnNamespaceAssignment.ProxyAutoConfigUri is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnNamespaceAssignment", "Uri VpnNamespaceAssignment.ProxyAutoConfigUri");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Networking.Vpn.VpnNamespaceInfo> NamespaceList
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<VpnNamespaceInfo> VpnNamespaceAssignment.NamespaceList is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnNamespaceAssignment", "IList<VpnNamespaceInfo> VpnNamespaceAssignment.NamespaceList");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public VpnNamespaceAssignment() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Vpn.VpnNamespaceAssignment", "VpnNamespaceAssignment.VpnNamespaceAssignment()");
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.VpnNamespaceAssignment.VpnNamespaceAssignment()
		// Forced skipping of method Windows.Networking.Vpn.VpnNamespaceAssignment.NamespaceList.set
		// Forced skipping of method Windows.Networking.Vpn.VpnNamespaceAssignment.NamespaceList.get
		// Forced skipping of method Windows.Networking.Vpn.VpnNamespaceAssignment.ProxyAutoConfigUri.set
		// Forced skipping of method Windows.Networking.Vpn.VpnNamespaceAssignment.ProxyAutoConfigUri.get
	}
}
