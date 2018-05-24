#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum BackgroundTaskCancellationReason 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Abort,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Terminating,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LoggingOff,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServicingUpdate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IdleTask,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Uninstall,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConditionLoss,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SystemPolicy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		QuietHoursEntered,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ExecutionTimeExceeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ResourceRevocation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EnergySaver,
		#endif
	}
	#endif
}
