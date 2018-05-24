#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.DirectX
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DirectXAlphaMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unspecified,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Premultiplied,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Straight,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ignore,
		#endif
	}
	#endif
}
