#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Credentials
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WebAccountPictureSize 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Size64x64,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Size208x208,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Size424x424,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Size1080x1080,
		#endif
	}
	#endif
}
