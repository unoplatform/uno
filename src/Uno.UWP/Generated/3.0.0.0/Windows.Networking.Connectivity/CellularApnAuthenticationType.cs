#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CellularApnAuthenticationType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pap,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Chap,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Mschapv2,
		#endif
	}
	#endif
}
