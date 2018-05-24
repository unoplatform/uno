#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AppRestartFailureReason 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RestartPending,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotInForeground,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidUser,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
	}
	#endif
}
