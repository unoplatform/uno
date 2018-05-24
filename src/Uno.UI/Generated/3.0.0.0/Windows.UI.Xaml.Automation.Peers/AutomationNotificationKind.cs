#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Peers
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AutomationNotificationKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ItemAdded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ItemRemoved,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ActionCompleted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ActionAborted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
	}
	#endif
}
