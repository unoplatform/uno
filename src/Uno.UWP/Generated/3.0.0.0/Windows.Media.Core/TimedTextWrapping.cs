#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TimedTextWrapping 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoWrap,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wrap,
		#endif
	}
	#endif
}
