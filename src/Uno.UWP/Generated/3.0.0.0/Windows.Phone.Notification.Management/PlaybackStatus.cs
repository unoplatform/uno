#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Phone.Notification.Management
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PlaybackStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TrackChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Stopped,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Playing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Paused,
		#endif
	}
	#endif
}
