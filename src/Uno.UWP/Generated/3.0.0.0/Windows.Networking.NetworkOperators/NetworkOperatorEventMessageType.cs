#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum NetworkOperatorEventMessageType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Gsm,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Cdma,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ussd,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DataPlanThresholdReached,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DataPlanReset,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DataPlanDeleted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProfileConnected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProfileDisconnected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RegisteredRoaming,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RegisteredHome,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TetheringEntitlementCheck,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TetheringOperationalStateChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TetheringNumberOfClientsChanged,
		#endif
	}
	#endif
}
