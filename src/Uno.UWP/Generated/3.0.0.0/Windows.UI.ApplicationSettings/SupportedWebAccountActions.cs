#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ApplicationSettings
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SupportedWebAccountActions 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Reconnect,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Remove,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ViewDetails,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Manage,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		More,
		#endif
	}
	#endif
}
