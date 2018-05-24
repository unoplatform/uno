#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFiDirect
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WiFiDirectPairingProcedure 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GroupOwnerNegotiation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Invitation,
		#endif
	}
	#endif
}
