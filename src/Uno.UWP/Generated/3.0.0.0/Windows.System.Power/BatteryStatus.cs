#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Power
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum BatteryStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotPresent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Discharging,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Idle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Charging,
		#endif
	}
	#endif
}
