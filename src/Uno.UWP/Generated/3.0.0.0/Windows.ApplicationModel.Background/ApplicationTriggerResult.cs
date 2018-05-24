#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ApplicationTriggerResult 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Allowed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CurrentlyRunning,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledByPolicy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownError,
		#endif
	}
	#endif
}
