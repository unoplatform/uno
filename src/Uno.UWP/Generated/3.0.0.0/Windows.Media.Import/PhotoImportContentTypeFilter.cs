#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Import
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PhotoImportContentTypeFilter 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OnlyImages,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OnlyVideos,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ImagesAndVideos,
		#endif
	}
	#endif
}
