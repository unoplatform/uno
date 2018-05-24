#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Perception.Spatial
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SpatialMovementRange 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoMovement,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Bounded,
		#endif
	}
	#endif
}
