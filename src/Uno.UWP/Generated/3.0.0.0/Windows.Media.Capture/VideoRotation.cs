#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum VideoRotation 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Clockwise90Degrees,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Clockwise180Degrees,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Clockwise270Degrees,
		#endif
	}
	#endif
}
