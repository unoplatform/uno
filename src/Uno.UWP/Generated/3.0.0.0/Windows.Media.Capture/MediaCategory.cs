#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MediaCategory 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Communications,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Media,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GameChat,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Speech,
		#endif
	}
	#endif
}
