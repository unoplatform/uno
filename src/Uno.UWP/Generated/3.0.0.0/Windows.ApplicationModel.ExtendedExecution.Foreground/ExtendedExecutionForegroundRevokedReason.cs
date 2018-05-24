#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.ExtendedExecution.Foreground
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ExtendedExecutionForegroundRevokedReason 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Resumed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SystemPolicy,
		#endif
	}
	#endif
}
