#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Compression
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CompressAlgorithm 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidAlgorithm,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NullAlgorithm,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Mszip,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Xpress,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		XpressHuff,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Lzms,
		#endif
	}
	#endif
}
