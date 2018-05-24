#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MediaStreamSourceClosedReason 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Done,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppReportedError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnsupportedProtectionSystem,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProtectionSystemFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnsupportedEncodingFormat,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MissingSampleRequestedEventHandler,
		#endif
	}
	#endif
}
