#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics.TraceReporting
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlatformDiagnosticTraceInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsAutoLogger
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PlatformDiagnosticTraceInfo.IsAutoLogger is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsExclusive
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PlatformDiagnosticTraceInfo.IsExclusive is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  long MaxTraceDurationFileTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member long PlatformDiagnosticTraceInfo.MaxTraceDurationFileTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticTracePriority Priority
		{
			get
			{
				throw new global::System.NotImplementedException("The member PlatformDiagnosticTracePriority PlatformDiagnosticTraceInfo.Priority is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong ProfileHash
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong PlatformDiagnosticTraceInfo.ProfileHash is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid ScenarioId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid PlatformDiagnosticTraceInfo.ScenarioId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticTraceInfo.ScenarioId.get
		// Forced skipping of method Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticTraceInfo.ProfileHash.get
		// Forced skipping of method Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticTraceInfo.IsExclusive.get
		// Forced skipping of method Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticTraceInfo.IsAutoLogger.get
		// Forced skipping of method Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticTraceInfo.MaxTraceDurationFileTime.get
		// Forced skipping of method Windows.System.Diagnostics.TraceReporting.PlatformDiagnosticTraceInfo.Priority.get
	}
}
