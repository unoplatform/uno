#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Spatial
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SpatialInteractionSourceKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hand,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Voice,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Controller,
		#endif
	}
	#endif
}
