#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.FileProperties
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ThumbnailType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Image,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Icon,
		#endif
	}
	#endif
}
