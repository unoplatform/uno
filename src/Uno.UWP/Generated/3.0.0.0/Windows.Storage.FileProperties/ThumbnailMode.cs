#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.FileProperties
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ThumbnailMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PicturesView,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VideosView,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MusicView,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DocumentsView,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ListView,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SingleItem,
		#endif
	}
	#endif
}
