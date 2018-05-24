#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum GraphicsTrustStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TrustNotRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TrustEstablished,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EnvironmentNotSupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DriverNotSupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DriverSigningFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownFailure,
		#endif
	}
	#endif
}
