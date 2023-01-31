#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GattLocalService 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalCharacteristic> Characteristics
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<GattLocalCharacteristic> GattLocalService.Characteristics is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CGattLocalCharacteristic%3E%20GattLocalService.Characteristics");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid Uuid
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid GattLocalService.Uuid is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Guid%20GattLocalService.Uuid");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalService.Uuid.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalCharacteristicResult> CreateCharacteristicAsync( global::System.Guid characteristicUuid,  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalCharacteristicParameters parameters)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattLocalCharacteristicResult> GattLocalService.CreateCharacteristicAsync(Guid characteristicUuid, GattLocalCharacteristicParameters parameters) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CGattLocalCharacteristicResult%3E%20GattLocalService.CreateCharacteristicAsync%28Guid%20characteristicUuid%2C%20GattLocalCharacteristicParameters%20parameters%29");
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalService.Characteristics.get
	}
}
