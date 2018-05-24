#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ManualFocusDistance 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Infinity,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hyperfocal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Nearest,
		#endif
	}
	#endif
}
