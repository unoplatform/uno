#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CoreInputViewOcclusionKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Docked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Floating,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Overlay,
		#endif
	}
	#endif
}
