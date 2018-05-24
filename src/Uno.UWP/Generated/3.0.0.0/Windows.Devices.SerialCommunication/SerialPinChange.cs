#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.SerialCommunication
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SerialPinChange 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BreakSignal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CarrierDetect,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ClearToSend,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DataSetReady,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RingIndicator,
		#endif
	}
	#endif
}
