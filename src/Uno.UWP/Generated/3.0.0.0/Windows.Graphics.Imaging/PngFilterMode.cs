#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Imaging
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PngFilterMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Automatic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Sub,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Up,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Average,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Paeth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Adaptive,
		#endif
	}
	#endif
}
