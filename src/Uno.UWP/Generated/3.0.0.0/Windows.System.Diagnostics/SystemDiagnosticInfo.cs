#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SystemDiagnosticInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.Diagnostics.SystemCpuUsage CpuUsage
		{
			get
			{
				throw new global::System.NotImplementedException("The member SystemCpuUsage SystemDiagnosticInfo.CpuUsage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.Diagnostics.SystemMemoryUsage MemoryUsage
		{
			get
			{
				throw new global::System.NotImplementedException("The member SystemMemoryUsage SystemDiagnosticInfo.MemoryUsage is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.Diagnostics.SystemDiagnosticInfo.MemoryUsage.get
		// Forced skipping of method Windows.System.Diagnostics.SystemDiagnosticInfo.CpuUsage.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Diagnostics.SystemDiagnosticInfo GetForCurrentSystem()
		{
			throw new global::System.NotImplementedException("The member SystemDiagnosticInfo SystemDiagnosticInfo.GetForCurrentSystem() is not implemented in Uno.");
		}
		#endif
	}
}
