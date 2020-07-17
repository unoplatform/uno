#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppResourceGroupMemoryReport 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.AppMemoryUsageLevel CommitUsageLevel
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppMemoryUsageLevel AppResourceGroupMemoryReport.CommitUsageLevel is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong CommitUsageLimit
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong AppResourceGroupMemoryReport.CommitUsageLimit is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong PrivateCommitUsage
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong AppResourceGroupMemoryReport.PrivateCommitUsage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong TotalCommitUsage
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong AppResourceGroupMemoryReport.TotalCommitUsage is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.AppResourceGroupMemoryReport.CommitUsageLimit.get
		// Forced skipping of method Windows.System.AppResourceGroupMemoryReport.CommitUsageLevel.get
		// Forced skipping of method Windows.System.AppResourceGroupMemoryReport.PrivateCommitUsage.get
		// Forced skipping of method Windows.System.AppResourceGroupMemoryReport.TotalCommitUsage.get
	}
}
