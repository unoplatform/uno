#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum LoggingOpcode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Info,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Start,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Stop,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Reply,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Resume,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Suspend,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Send,
		#endif
	}
	#endif
}
