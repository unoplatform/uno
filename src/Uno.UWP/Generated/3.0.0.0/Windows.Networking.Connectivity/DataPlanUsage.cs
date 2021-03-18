#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DataPlanUsage 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset LastSyncTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset DataPlanUsage.LastSyncTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint MegabytesUsed
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint DataPlanUsage.MegabytesUsed is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Connectivity.DataPlanUsage.MegabytesUsed.get
		// Forced skipping of method Windows.Networking.Connectivity.DataPlanUsage.LastSyncTime.get
	}
}
