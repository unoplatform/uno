#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PosPrinterLineStyle 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SingleSolid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DoubleSolid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Broken,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Chain,
		#endif
	}
	#endif
}
