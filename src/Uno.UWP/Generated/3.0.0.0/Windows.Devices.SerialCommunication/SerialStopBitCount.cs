#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.SerialCommunication
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SerialStopBitCount 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		One,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OnePointFive,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Two,
		#endif
	}
	#endif
}
