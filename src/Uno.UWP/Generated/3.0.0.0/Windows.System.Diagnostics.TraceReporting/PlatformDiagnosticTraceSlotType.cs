#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics.TraceReporting
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PlatformDiagnosticTraceSlotType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Alternative,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AlwaysOn,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Mini,
		#endif
	}
	#endif
}
