#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Display
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DisplayMonitorUsageKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Standard,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HeadMounted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SpecialPurpose,
		#endif
	}
	#endif
}
