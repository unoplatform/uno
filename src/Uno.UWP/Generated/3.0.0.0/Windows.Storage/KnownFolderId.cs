#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum KnownFolderId 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppCaptures,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CameraRoll,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DocumentsLibrary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HomeGroup,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MediaServerDevices,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MusicLibrary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Objects3D,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PicturesLibrary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Playlists,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RecordedCalls,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RemovableDevices,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SavedPictures,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Screenshots,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VideosLibrary,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AllAppMods,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CurrentAppMods,
		#endif
	}
	#endif
}
