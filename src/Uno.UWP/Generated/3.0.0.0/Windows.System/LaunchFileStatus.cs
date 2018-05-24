#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum LaunchFileStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppUnavailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeniedByPolicy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FileTypeNotSupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
	}
	#endif
}
