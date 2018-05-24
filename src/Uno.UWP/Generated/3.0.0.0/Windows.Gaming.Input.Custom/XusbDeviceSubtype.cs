#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input.Custom
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum XusbDeviceSubtype 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Gamepad,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ArcadePad,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ArcadeStick,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FlightStick,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Wheel,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Guitar,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GuitarAlternate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GuitarBass,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DrumKit,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DancePad,
		#endif
	}
	#endif
}
