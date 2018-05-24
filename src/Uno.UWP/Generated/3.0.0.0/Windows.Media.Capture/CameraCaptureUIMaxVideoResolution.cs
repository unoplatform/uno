#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CameraCaptureUIMaxVideoResolution 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HighestAvailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LowDefinition,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StandardDefinition,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HighDefinition,
		#endif
	}
	#endif
}
