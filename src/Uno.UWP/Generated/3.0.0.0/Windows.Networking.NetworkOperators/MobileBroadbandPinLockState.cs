#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MobileBroadbandPinLockState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unlocked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PinRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PinUnblockKeyRequired,
		#endif
	}
	#endif
}
