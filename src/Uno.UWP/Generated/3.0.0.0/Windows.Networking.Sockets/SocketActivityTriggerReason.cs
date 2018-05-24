#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SocketActivityTriggerReason 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SocketActivity,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConnectionAccepted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		KeepAliveTimerExpired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SocketClosed,
		#endif
	}
	#endif
}
