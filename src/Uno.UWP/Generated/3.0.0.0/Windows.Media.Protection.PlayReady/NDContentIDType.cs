#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection.PlayReady
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum NDContentIDType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		KeyID,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PlayReadyObject,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Custom,
		#endif
	}
	#endif
}
