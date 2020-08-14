#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IVpnProfile 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool AlwaysOn
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IList<global::Windows.Networking.Vpn.VpnAppId> AppTriggers
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IList<global::Windows.Networking.Vpn.VpnDomainNameInfo> DomainNameInfoList
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string ProfileName
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool RememberCredentials
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IList<global::Windows.Networking.Vpn.VpnRoute> Routes
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IList<global::Windows.Networking.Vpn.VpnTrafficFilter> TrafficFilters
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.IVpnProfile.ProfileName.get
		// Forced skipping of method Windows.Networking.Vpn.IVpnProfile.ProfileName.set
		// Forced skipping of method Windows.Networking.Vpn.IVpnProfile.AppTriggers.get
		// Forced skipping of method Windows.Networking.Vpn.IVpnProfile.Routes.get
		// Forced skipping of method Windows.Networking.Vpn.IVpnProfile.DomainNameInfoList.get
		// Forced skipping of method Windows.Networking.Vpn.IVpnProfile.TrafficFilters.get
		// Forced skipping of method Windows.Networking.Vpn.IVpnProfile.RememberCredentials.get
		// Forced skipping of method Windows.Networking.Vpn.IVpnProfile.RememberCredentials.set
		// Forced skipping of method Windows.Networking.Vpn.IVpnProfile.AlwaysOn.get
		// Forced skipping of method Windows.Networking.Vpn.IVpnProfile.AlwaysOn.set
	}
}
