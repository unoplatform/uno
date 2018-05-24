#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ElementSoundKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Focus,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Invoke,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Show,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hide,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MovePrevious,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MoveNext,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GoBack,
		#endif
	}
	#endif
}
