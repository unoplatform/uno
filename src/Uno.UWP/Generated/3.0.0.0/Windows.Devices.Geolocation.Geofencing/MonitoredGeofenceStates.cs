#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Geolocation.Geofencing
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MonitoredGeofenceStates 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Entered,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Exited,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Removed,
		#endif
	}
	#endif
}
