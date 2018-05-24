#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CoreProcessEventsOption 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProcessOneAndAllPending,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProcessOneIfPresent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProcessUntilQuit,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProcessAllIfPresent,
		#endif
	}
	#endif
}
