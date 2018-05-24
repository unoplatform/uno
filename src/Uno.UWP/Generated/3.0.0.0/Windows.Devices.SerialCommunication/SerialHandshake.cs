#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.SerialCommunication
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SerialHandshake 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RequestToSend,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		XOnXOff,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RequestToSendXOnXOff,
		#endif
	}
	#endif
}
