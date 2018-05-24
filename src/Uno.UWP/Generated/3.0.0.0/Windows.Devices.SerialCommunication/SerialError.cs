#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.SerialCommunication
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SerialError 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Frame,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BufferOverrun,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReceiveFull,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReceiveParity,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TransmitFull,
		#endif
	}
	#endif
}
