#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DataPlanStatus 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint? DataLimitInMegabytes
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint? DataPlanStatus.DataLimitInMegabytes is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.DataPlanUsage DataPlanUsage
		{
			get
			{
				throw new global::System.NotImplementedException("The member DataPlanUsage DataPlanStatus.DataPlanUsage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong? InboundBitsPerSecond
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong? DataPlanStatus.InboundBitsPerSecond is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint? MaxTransferSizeInMegabytes
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint? DataPlanStatus.MaxTransferSizeInMegabytes is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset? NextBillingCycle
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset? DataPlanStatus.NextBillingCycle is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong? OutboundBitsPerSecond
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong? DataPlanStatus.OutboundBitsPerSecond is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Connectivity.DataPlanStatus.DataPlanUsage.get
		// Forced skipping of method Windows.Networking.Connectivity.DataPlanStatus.DataLimitInMegabytes.get
		// Forced skipping of method Windows.Networking.Connectivity.DataPlanStatus.InboundBitsPerSecond.get
		// Forced skipping of method Windows.Networking.Connectivity.DataPlanStatus.OutboundBitsPerSecond.get
		// Forced skipping of method Windows.Networking.Connectivity.DataPlanStatus.NextBillingCycle.get
		// Forced skipping of method Windows.Networking.Connectivity.DataPlanStatus.MaxTransferSizeInMegabytes.get
	}
}
