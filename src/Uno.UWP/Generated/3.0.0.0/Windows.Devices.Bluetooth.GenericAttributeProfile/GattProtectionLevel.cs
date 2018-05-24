#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum GattProtectionLevel 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Plain,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AuthenticationRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EncryptionRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EncryptionAndAuthenticationRequired,
		#endif
	}
	#endif
}
