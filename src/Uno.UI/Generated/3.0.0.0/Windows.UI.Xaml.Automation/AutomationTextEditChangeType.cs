#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AutomationTextEditChangeType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AutoCorrect,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Composition,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CompositionFinalized,
		#endif
	}
	#endif
}
