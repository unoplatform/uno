#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum VpnNativeProtocolType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pptp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		L2tp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IpsecIkev2,
		#endif
	}
	#endif
}
