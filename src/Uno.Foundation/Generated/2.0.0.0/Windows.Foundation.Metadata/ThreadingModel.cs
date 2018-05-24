#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Metadata
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ThreadingModel 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		STA,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MTA,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Both,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidThreading,
		#endif
	}
	#endif
}
