#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppMemoryReport 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong PeakPrivateCommitUsage
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong AppMemoryReport.PeakPrivateCommitUsage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong PrivateCommitUsage
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong AppMemoryReport.PrivateCommitUsage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong TotalCommitLimit
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong AppMemoryReport.TotalCommitLimit is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong TotalCommitUsage
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong AppMemoryReport.TotalCommitUsage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong ExpectedTotalCommitLimit
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong AppMemoryReport.ExpectedTotalCommitLimit is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.AppMemoryReport.PrivateCommitUsage.get
		// Forced skipping of method Windows.System.AppMemoryReport.PeakPrivateCommitUsage.get
		// Forced skipping of method Windows.System.AppMemoryReport.TotalCommitUsage.get
		// Forced skipping of method Windows.System.AppMemoryReport.TotalCommitLimit.get
		// Forced skipping of method Windows.System.AppMemoryReport.ExpectedTotalCommitLimit.get
	}
}
