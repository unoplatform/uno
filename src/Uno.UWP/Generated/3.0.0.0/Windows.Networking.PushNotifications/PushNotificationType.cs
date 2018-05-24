#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.PushNotifications
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PushNotificationType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Toast,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Tile,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Badge,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Raw,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TileFlyout,
		#endif
	}
	#endif
}
