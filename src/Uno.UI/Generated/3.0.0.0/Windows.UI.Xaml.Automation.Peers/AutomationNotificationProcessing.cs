#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AutomationNotificationProcessing 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ImportantAll,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ImportantMostRecent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		All,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MostRecent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CurrentThenMostRecent,
		#endif
	}
	#endif
}
