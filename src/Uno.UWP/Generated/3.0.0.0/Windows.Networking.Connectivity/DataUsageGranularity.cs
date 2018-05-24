#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Connectivity
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DataUsageGranularity 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PerMinute,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PerHour,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PerDay,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Total,
		#endif
	}
	#endif
}
