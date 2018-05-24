#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Input.Inking.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CoreWetStrokeDisposition 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Inking,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Completed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Canceled,
		#endif
	}
	#endif
}
