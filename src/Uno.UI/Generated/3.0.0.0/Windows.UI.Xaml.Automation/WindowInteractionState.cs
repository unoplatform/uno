#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WindowInteractionState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Running,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Closing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReadyForUserInteraction,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BlockedByModalWindow,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotResponding,
		#endif
	}
	#endif
}
