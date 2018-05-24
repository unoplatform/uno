#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Vpn
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum VpnManagementConnectionStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Disconnected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Disconnecting,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Connected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Connecting,
		#endif
	}
	#endif
}
