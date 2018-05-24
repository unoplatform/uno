#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum UnifiedPosPowerReportingType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownPowerReportingType,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Standard,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Advanced,
		#endif
	}
	#endif
}
