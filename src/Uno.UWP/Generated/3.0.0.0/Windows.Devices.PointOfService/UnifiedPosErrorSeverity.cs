#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum UnifiedPosErrorSeverity 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownErrorSeverity,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Warning,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Recoverable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unrecoverable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AssistanceRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Fatal,
		#endif
	}
	#endif
}
