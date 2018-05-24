#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum LineSpacingRule 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Undefined,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Single,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OneAndHalf,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Double,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AtLeast,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Exactly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Multiple,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Percent,
		#endif
	}
	#endif
}
