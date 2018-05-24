#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DiagnosticActionState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Initializing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Downloading,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VerifyingTrust,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Detecting,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Resolving,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VerifyingResolution,
		#endif
	}
	#endif
}
