#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProcessDiskUsageReport 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  long BytesReadCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member long ProcessDiskUsageReport.BytesReadCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  long BytesWrittenCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member long ProcessDiskUsageReport.BytesWrittenCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  long OtherBytesCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member long ProcessDiskUsageReport.OtherBytesCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  long OtherOperationCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member long ProcessDiskUsageReport.OtherOperationCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  long ReadOperationCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member long ProcessDiskUsageReport.ReadOperationCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  long WriteOperationCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member long ProcessDiskUsageReport.WriteOperationCount is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.Diagnostics.ProcessDiskUsageReport.ReadOperationCount.get
		// Forced skipping of method Windows.System.Diagnostics.ProcessDiskUsageReport.WriteOperationCount.get
		// Forced skipping of method Windows.System.Diagnostics.ProcessDiskUsageReport.OtherOperationCount.get
		// Forced skipping of method Windows.System.Diagnostics.ProcessDiskUsageReport.BytesReadCount.get
		// Forced skipping of method Windows.System.Diagnostics.ProcessDiskUsageReport.BytesWrittenCount.get
		// Forced skipping of method Windows.System.Diagnostics.ProcessDiskUsageReport.OtherBytesCount.get
	}
}
