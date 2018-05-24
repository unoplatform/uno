#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Imaging
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TiffCompressionMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Automatic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ccitt3,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ccitt4,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Lzw,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Zip,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LzwhDifferencing,
		#endif
	}
	#endif
}
