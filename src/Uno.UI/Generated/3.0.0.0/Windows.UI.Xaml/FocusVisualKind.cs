#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum FocusVisualKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DottedLine,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HighVisibility,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Reveal,
		#endif
	}
	#endif
}
