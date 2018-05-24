#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AutomationFlowDirections 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RightToLeft,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BottomToTop,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Vertical,
		#endif
	}
	#endif
}
