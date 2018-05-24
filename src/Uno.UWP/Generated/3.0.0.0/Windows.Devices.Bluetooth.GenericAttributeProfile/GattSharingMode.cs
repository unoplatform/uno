#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum GattSharingMode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unspecified,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Exclusive,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SharedReadOnly,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SharedReadAndWrite,
		#endif
	}
	#endif
}
