#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.ExtendedExecution.Foreground
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ExtendedExecutionForegroundReason 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unspecified,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SavingData,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BackgroundAudio,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unconstrained,
		#endif
	}
	#endif
}
