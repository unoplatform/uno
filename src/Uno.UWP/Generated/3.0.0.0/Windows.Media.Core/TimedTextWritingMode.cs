#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TimedTextWritingMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LeftRightTopBottom,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RightLeftTopBottom,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TopBottomRightLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TopBottomLeftRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LeftRight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RightLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TopBottom,
		#endif
	}
	#endif
}
