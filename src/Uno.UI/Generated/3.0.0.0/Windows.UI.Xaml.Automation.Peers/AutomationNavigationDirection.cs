#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AutomationNavigationDirection 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Parent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NextSibling,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PreviousSibling,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FirstChild,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LastChild,
		#endif
	}
	#endif
}
