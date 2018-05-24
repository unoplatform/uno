#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Gaming.Input
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial struct GamepadReading 
	{
		// Forced skipping of method Windows.Gaming.Input.GamepadReading.GamepadReading()
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  ulong Timestamp;
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  global::Windows.Gaming.Input.GamepadButtons Buttons;
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  double LeftTrigger;
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  double RightTrigger;
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  double LeftThumbstickX;
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  double LeftThumbstickY;
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  double RightThumbstickX;
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  double RightThumbstickY;
		#endif
	}
}
