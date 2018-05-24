#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CoreAcceleratorKeyEventType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Character,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeadCharacter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		KeyDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		KeyUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SystemCharacter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SystemDeadCharacter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SystemKeyDown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SystemKeyUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnicodeCharacter,
		#endif
	}
	#endif
}
