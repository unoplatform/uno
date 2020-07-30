#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics.TraceReporting
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlatformDiagnosticTraceRuntimeInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  long EtwRuntimeFileTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member long PlatformDiagnosticTraceRuntimeInfo.EtwRuntimeFileTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  long RuntimeFileTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member long PlatformDiagnosticTraceRuntimeInfo.RuntimeFileTime is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticTraceRuntimeInfo.RuntimeFileTime.get
		// Forced skipping of method Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticTraceRuntimeInfo.EtwRuntimeFileTime.get
	}
}
