#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Sms
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SmsBroadcastType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CmasPresidential,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CmasExtreme,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CmasSevere,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CmasAmber,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CmasTest,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EUAlert1,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EUAlert2,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EUAlert3,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EUAlertAmber,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EUAlertInfo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EtwsEarthquake,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EtwsTsunami,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EtwsTsunamiAndEarthquake,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LatAlertLocal,
		#endif
	}
	#endif
}
