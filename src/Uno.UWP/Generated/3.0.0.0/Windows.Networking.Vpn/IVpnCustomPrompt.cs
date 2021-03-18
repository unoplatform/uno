#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IVpnCustomPrompt 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool Bordered
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool Compulsory
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Label
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Networking.Vpn.IVpnCustomPrompt.Label.set
		// Forced skipping of method Windows.Networking.Vpn.IVpnCustomPrompt.Label.get
		// Forced skipping of method Windows.Networking.Vpn.IVpnCustomPrompt.Compulsory.set
		// Forced skipping of method Windows.Networking.Vpn.IVpnCustomPrompt.Compulsory.get
		// Forced skipping of method Windows.Networking.Vpn.IVpnCustomPrompt.Bordered.set
		// Forced skipping of method Windows.Networking.Vpn.IVpnCustomPrompt.Bordered.get
	}
}
