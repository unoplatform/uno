#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Devices
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum FocusMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Auto,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Single,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Continuous,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Manual,
		#endif
	}
	#endif
}
