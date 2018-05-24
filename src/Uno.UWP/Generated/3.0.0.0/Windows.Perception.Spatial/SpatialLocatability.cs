#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Perception.Spatial
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SpatialLocatability 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unavailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OrientationOnly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PositionalTrackingActivating,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PositionalTrackingActive,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PositionalTrackingInhibited,
		#endif
	}
	#endif
}
