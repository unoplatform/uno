#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.FileProperties
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ThumbnailOptions 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReturnOnlyIfCached,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ResizeThumbnail,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseCurrentScale,
		#endif
	}
	#endif
}
