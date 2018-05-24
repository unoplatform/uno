#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CoreWindowActivationMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Deactivated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ActivatedNotForeground,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ActivatedInForeground,
		#endif
	}
	#endif
}
