#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProcessDiagnosticInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.Diagnostics.ProcessCpuUsage CpuUsage
		{
			get
			{
				throw new global::System.NotImplementedException("The member ProcessCpuUsage ProcessDiagnosticInfo.CpuUsage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.Diagnostics.ProcessDiskUsage DiskUsage
		{
			get
			{
				throw new global::System.NotImplementedException("The member ProcessDiskUsage ProcessDiagnosticInfo.DiskUsage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ExecutableFileName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ProcessDiagnosticInfo.ExecutableFileName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.Diagnostics.ProcessMemoryUsage MemoryUsage
		{
			get
			{
				throw new global::System.NotImplementedException("The member ProcessMemoryUsage ProcessDiagnosticInfo.MemoryUsage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.Diagnostics.ProcessDiagnosticInfo Parent
		{
			get
			{
				throw new global::System.NotImplementedException("The member ProcessDiagnosticInfo ProcessDiagnosticInfo.Parent is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint ProcessId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint ProcessDiagnosticInfo.ProcessId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset ProcessStartTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset ProcessDiagnosticInfo.ProcessStartTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPackaged
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ProcessDiagnosticInfo.IsPackaged is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.Diagnostics.ProcessDiagnosticInfo.ProcessId.get
		// Forced skipping of method Windows.System.Diagnostics.ProcessDiagnosticInfo.ExecutableFileName.get
		// Forced skipping of method Windows.System.Diagnostics.ProcessDiagnosticInfo.Parent.get
		// Forced skipping of method Windows.System.Diagnostics.ProcessDiagnosticInfo.ProcessStartTime.get
		// Forced skipping of method Windows.System.Diagnostics.ProcessDiagnosticInfo.DiskUsage.get
		// Forced skipping of method Windows.System.Diagnostics.ProcessDiagnosticInfo.MemoryUsage.get
		// Forced skipping of method Windows.System.Diagnostics.ProcessDiagnosticInfo.CpuUsage.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.System.AppDiagnosticInfo> GetAppDiagnosticInfos()
		{
			throw new global::System.NotImplementedException("The member IList<AppDiagnosticInfo> ProcessDiagnosticInfo.GetAppDiagnosticInfos() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.System.Diagnostics.ProcessDiagnosticInfo.IsPackaged.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Diagnostics.ProcessDiagnosticInfo TryGetForProcessId( uint processId)
		{
			throw new global::System.NotImplementedException("The member ProcessDiagnosticInfo ProcessDiagnosticInfo.TryGetForProcessId(uint processId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.System.Diagnostics.ProcessDiagnosticInfo> GetForProcesses()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<ProcessDiagnosticInfo> ProcessDiagnosticInfo.GetForProcesses() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Diagnostics.ProcessDiagnosticInfo GetForCurrentProcess()
		{
			throw new global::System.NotImplementedException("The member ProcessDiagnosticInfo ProcessDiagnosticInfo.GetForCurrentProcess() is not implemented in Uno.");
		}
		#endif
	}
}
