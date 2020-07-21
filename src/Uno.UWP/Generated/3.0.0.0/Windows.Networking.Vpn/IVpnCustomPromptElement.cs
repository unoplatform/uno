#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IVpnCustomPromptElement 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool Compulsory
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string DisplayName
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool Emphasized
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.IVpnCustomPromptElement.DisplayName.set
		// Forced skipping of method Windows.Networking.Vpn.IVpnCustomPromptElement.DisplayName.get
		// Forced skipping of method Windows.Networking.Vpn.IVpnCustomPromptElement.Compulsory.set
		// Forced skipping of method Windows.Networking.Vpn.IVpnCustomPromptElement.Compulsory.get
		// Forced skipping of method Windows.Networking.Vpn.IVpnCustomPromptElement.Emphasized.set
		// Forced skipping of method Windows.Networking.Vpn.IVpnCustomPromptElement.Emphasized.get
	}
}
