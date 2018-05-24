#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MediaSourceState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Initial,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Opening,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Opened,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Failed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Closed,
		#endif
	}
	#endif
}
