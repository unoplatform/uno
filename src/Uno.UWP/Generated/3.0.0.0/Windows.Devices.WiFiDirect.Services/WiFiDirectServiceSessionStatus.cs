#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFiDirect.Services
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WiFiDirectServiceSessionStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Closed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Initiated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Requested,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Open,
		#endif
	}
	#endif
}
