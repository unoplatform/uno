#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Perception.Spatial
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SpatialLookDirectionRange 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ForwardOnly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Omnidirectional,
		#endif
	}
	#endif
}
