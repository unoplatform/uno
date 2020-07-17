#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MobileBroadbandModemConfiguration 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string HomeProviderId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MobileBroadbandModemConfiguration.HomeProviderId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string HomeProviderName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MobileBroadbandModemConfiguration.HomeProviderName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.MobileBroadbandUicc Uicc
		{
			get
			{
				throw new global::System.NotImplementedException("The member MobileBroadbandUicc MobileBroadbandModemConfiguration.Uicc is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.NetworkOperators.MobileBroadbandSarManager SarManager
		{
			get
			{
				throw new global::System.NotImplementedException("The member MobileBroadbandSarManager MobileBroadbandModemConfiguration.SarManager is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandModemConfiguration.Uicc.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandModemConfiguration.HomeProviderId.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandModemConfiguration.HomeProviderName.get
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandModemConfiguration.SarManager.get
	}
}
