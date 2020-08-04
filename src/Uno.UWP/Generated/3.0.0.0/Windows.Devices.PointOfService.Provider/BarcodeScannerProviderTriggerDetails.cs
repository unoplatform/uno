#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BarcodeScannerProviderTriggerDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.PointOfService.Provider.BarcodeScannerProviderConnection Connection
		{
			get
			{
				throw new global::System.NotImplementedException("The member BarcodeScannerProviderConnection BarcodeScannerProviderTriggerDetails.Connection is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.PointOfService.Provider.BarcodeScannerProviderTriggerDetails.Connection.get
	}
}
