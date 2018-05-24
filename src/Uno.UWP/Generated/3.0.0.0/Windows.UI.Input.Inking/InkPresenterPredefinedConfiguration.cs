#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum InkPresenterPredefinedConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SimpleSinglePointer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SimpleMultiplePointer,
		#endif
	}
	#endif
}
