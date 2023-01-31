#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GattCharacteristicNotificationTriggerDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic Characteristic
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattCharacteristic GattCharacteristicNotificationTriggerDetails.Characteristic is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=GattCharacteristic%20GattCharacteristicNotificationTriggerDetails.Characteristic");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer GattCharacteristicNotificationTriggerDetails.Value is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IBuffer%20GattCharacteristicNotificationTriggerDetails.Value");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothError Error
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothError GattCharacteristicNotificationTriggerDetails.Error is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=BluetoothError%20GattCharacteristicNotificationTriggerDetails.Error");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.Background.BluetoothEventTriggeringMode EventTriggeringMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothEventTriggeringMode GattCharacteristicNotificationTriggerDetails.EventTriggeringMode is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=BluetoothEventTriggeringMode%20GattCharacteristicNotificationTriggerDetails.EventTriggeringMode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattValueChangedEventArgs> ValueChangedEvents
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<GattValueChangedEventArgs> GattCharacteristicNotificationTriggerDetails.ValueChangedEvents is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CGattValueChangedEventArgs%3E%20GattCharacteristicNotificationTriggerDetails.ValueChangedEvents");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.Background.GattCharacteristicNotificationTriggerDetails.Characteristic.get
		// Forced skipping of method Windows.Devices.Bluetooth.Background.GattCharacteristicNotificationTriggerDetails.Value.get
		// Forced skipping of method Windows.Devices.Bluetooth.Background.GattCharacteristicNotificationTriggerDetails.Error.get
		// Forced skipping of method Windows.Devices.Bluetooth.Background.GattCharacteristicNotificationTriggerDetails.EventTriggeringMode.get
		// Forced skipping of method Windows.Devices.Bluetooth.Background.GattCharacteristicNotificationTriggerDetails.ValueChangedEvents.get
	}
}
