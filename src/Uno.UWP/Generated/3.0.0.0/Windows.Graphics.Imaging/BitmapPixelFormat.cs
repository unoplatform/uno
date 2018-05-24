#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Imaging
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum BitmapPixelFormat 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rgba16,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rgba8,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Gray16,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Gray8,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Bgra8,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Nv12,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		P010,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Yuy2,
		#endif
	}
	#endif
}
