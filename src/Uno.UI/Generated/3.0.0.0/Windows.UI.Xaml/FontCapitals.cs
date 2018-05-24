#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum FontCapitals 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Normal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AllSmallCaps,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SmallCaps,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AllPetiteCaps,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PetiteCaps,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unicase,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Titling,
		#endif
	}
	#endif
}
