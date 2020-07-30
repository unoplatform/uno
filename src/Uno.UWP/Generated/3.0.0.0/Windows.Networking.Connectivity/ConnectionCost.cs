#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ConnectionCost 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool ApproachingDataLimit
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ConnectionCost.ApproachingDataLimit is not implemented in Uno.");
			}
		}
		#endif
		#if false || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Connectivity.NetworkCostType NetworkCostType
		{
			get
			{
				throw new global::System.NotImplementedException("The member NetworkCostType ConnectionCost.NetworkCostType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool OverDataLimit
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ConnectionCost.OverDataLimit is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Roaming
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ConnectionCost.Roaming is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool BackgroundDataUsageRestricted
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ConnectionCost.BackgroundDataUsageRestricted is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Connectivity.ConnectionCost.NetworkCostType.get
		// Forced skipping of method Windows.Networking.Connectivity.ConnectionCost.Roaming.get
		// Forced skipping of method Windows.Networking.Connectivity.ConnectionCost.OverDataLimit.get
		// Forced skipping of method Windows.Networking.Connectivity.ConnectionCost.ApproachingDataLimit.get
		// Forced skipping of method Windows.Networking.Connectivity.ConnectionCost.BackgroundDataUsageRestricted.get
	}
}
