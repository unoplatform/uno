#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GattCharacteristicNotificationTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic Characteristic
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattCharacteristic GattCharacteristicNotificationTrigger.Characteristic is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.Background.BluetoothEventTriggeringMode EventTriggeringMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothEventTriggeringMode GattCharacteristicNotificationTrigger.EventTriggeringMode is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public GattCharacteristicNotificationTrigger( global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic characteristic,  global::Windows.Devices.Bluetooth.Background.BluetoothEventTriggeringMode eventTriggeringMode) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.GattCharacteristicNotificationTrigger", "GattCharacteristicNotificationTrigger.GattCharacteristicNotificationTrigger(GattCharacteristic characteristic, BluetoothEventTriggeringMode eventTriggeringMode)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.GattCharacteristicNotificationTrigger.GattCharacteristicNotificationTrigger(Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic, Windows.Devices.Bluetooth.Background.BluetoothEventTriggeringMode)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public GattCharacteristicNotificationTrigger( global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic characteristic) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.GattCharacteristicNotificationTrigger", "GattCharacteristicNotificationTrigger.GattCharacteristicNotificationTrigger(GattCharacteristic characteristic)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.GattCharacteristicNotificationTrigger.GattCharacteristicNotificationTrigger(Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic)
		// Forced skipping of method Windows.ApplicationModel.Background.GattCharacteristicNotificationTrigger.Characteristic.get
		// Forced skipping of method Windows.ApplicationModel.Background.GattCharacteristicNotificationTrigger.EventTriggeringMode.get
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
