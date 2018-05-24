#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PosPrinterMarkFeedKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToTakeUp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToCutter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToCurrentTopOfForm,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ToNextTopOfForm,
		#endif
	}
	#endif
}
