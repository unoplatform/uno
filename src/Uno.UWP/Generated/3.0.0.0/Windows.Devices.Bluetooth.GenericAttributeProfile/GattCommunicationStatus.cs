#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum GattCommunicationStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unreachable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProtocolError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AccessDenied,
		#endif
	}
	#endif
}
