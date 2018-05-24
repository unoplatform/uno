#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AnimationDirection 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Normal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Reverse,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Alternate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AlternateReverse,
		#endif
	}
	#endif
}
