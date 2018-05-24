#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TabLeader 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Spaces,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Dots,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Dashes,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Lines,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ThickLines,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Equals,
		#endif
	}
	#endif
}
