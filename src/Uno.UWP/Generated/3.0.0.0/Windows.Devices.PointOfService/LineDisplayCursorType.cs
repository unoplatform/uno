#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum LineDisplayCursorType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Block,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HalfBlock,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Underline,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Reverse,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
	}
	#endif
}
