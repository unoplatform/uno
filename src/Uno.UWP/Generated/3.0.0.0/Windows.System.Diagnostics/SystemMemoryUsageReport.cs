#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SystemMemoryUsageReport 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong AvailableSizeInBytes
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong SystemMemoryUsageReport.AvailableSizeInBytes is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong CommittedSizeInBytes
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong SystemMemoryUsageReport.CommittedSizeInBytes is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong TotalPhysicalSizeInBytes
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong SystemMemoryUsageReport.TotalPhysicalSizeInBytes is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.Diagnostics.SystemMemoryUsageReport.TotalPhysicalSizeInBytes.get
		// Forced skipping of method Windows.System.Diagnostics.SystemMemoryUsageReport.AvailableSizeInBytes.get
		// Forced skipping of method Windows.System.Diagnostics.SystemMemoryUsageReport.CommittedSizeInBytes.get
	}
}
