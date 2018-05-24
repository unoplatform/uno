#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Proximity
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TriggeredConnectState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PeerFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Listening,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Connecting,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Completed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Canceled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Failed,
		#endif
	}
	#endif
}
