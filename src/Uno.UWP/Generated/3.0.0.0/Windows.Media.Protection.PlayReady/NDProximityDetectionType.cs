#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum NDProximityDetectionType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UDP,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TCP,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TransportAgnostic,
		#endif
	}
	#endif
}
