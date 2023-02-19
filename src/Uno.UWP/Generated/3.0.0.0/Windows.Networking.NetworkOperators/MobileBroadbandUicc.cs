#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MobileBroadbandUicc 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string SimIccId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string MobileBroadbandUicc.SimIccId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20MobileBroadbandUicc.SimIccId");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.NetworkOperators.MobileBroadbandUicc.SimIccId.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Networking.NetworkOperators.MobileBroadbandUiccAppsResult> GetUiccAppsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MobileBroadbandUiccAppsResult> MobileBroadbandUicc.GetUiccAppsAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CMobileBroadbandUiccAppsResult%3E%20MobileBroadbandUicc.GetUiccAppsAsync%28%29");
		}
		#endif
	}
}
