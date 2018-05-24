#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum VisitStateChange 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TrackingLost,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Arrived,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Departed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OtherMovement,
		#endif
	}
	#endif
}
