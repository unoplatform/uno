#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CameraCaptureUIMaxPhotoResolution 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HighestAvailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VerySmallQvga,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SmallVga,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MediumXga,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Large3M,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VeryLarge5M,
		#endif
	}
	#endif
}
