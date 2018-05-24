#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PosPrinterMapMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Dots,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Twips,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		English,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Metric,
		#endif
	}
	#endif
}
