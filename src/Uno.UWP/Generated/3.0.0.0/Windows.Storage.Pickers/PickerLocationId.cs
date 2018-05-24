#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Pickers
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PickerLocationId 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DocumentsLibrary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ComputerFolder,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Desktop,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Downloads,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HomeGroup,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MusicLibrary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PicturesLibrary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VideosLibrary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Objects3D,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unspecified,
		#endif
	}
	#endif
}
