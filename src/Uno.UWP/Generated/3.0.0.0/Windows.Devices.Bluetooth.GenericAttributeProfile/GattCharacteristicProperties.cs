#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum GattCharacteristicProperties 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Broadcast,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Read,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WriteWithoutResponse,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Write,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Notify,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Indicate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AuthenticatedSignedWrites,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ExtendedProperties,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReliableWrites,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WritableAuxiliaries,
		#endif
	}
	#endif
}
