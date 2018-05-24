#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.DirectX.Direct3D11
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum Direct3DUsage 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Immutable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Dynamic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Staging,
		#endif
	}
	#endif
}
