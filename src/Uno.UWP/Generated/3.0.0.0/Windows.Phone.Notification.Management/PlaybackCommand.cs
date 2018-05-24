#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Notification.Management
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PlaybackCommand 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Play,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pause,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Stop,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Record,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FastForward,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rewind,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Next,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Previous,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChannelUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChannelDown,
		#endif
	}
	#endif
}
