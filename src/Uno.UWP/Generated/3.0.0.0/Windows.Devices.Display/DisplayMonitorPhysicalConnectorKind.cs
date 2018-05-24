#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Display
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DisplayMonitorPhysicalConnectorKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HD15,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AnalogTV,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Dvi,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hdmi,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Lvds,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Sdi,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisplayPort,
		#endif
	}
	#endif
}
