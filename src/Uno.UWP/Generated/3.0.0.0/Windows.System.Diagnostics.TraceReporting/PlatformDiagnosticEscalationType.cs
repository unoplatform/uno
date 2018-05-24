#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics.TraceReporting
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PlatformDiagnosticEscalationType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OnCompletion,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OnFailure,
		#endif
	}
	#endif
}
