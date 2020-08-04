#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BluetoothClassOfDevice 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothMajorClass MajorClass
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothMajorClass BluetoothClassOfDevice.MajorClass is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothMinorClass MinorClass
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothMinorClass BluetoothClassOfDevice.MinorClass is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint RawValue
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint BluetoothClassOfDevice.RawValue is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothServiceCapabilities ServiceCapabilities
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothServiceCapabilities BluetoothClassOfDevice.ServiceCapabilities is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothClassOfDevice.RawValue.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothClassOfDevice.MajorClass.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothClassOfDevice.MinorClass.get
		// Forced skipping of method Windows.Devices.Bluetooth.BluetoothClassOfDevice.ServiceCapabilities.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Bluetooth.BluetoothClassOfDevice FromRawValue( uint rawValue)
		{
			throw new global::System.NotImplementedException("The member BluetoothClassOfDevice BluetoothClassOfDevice.FromRawValue(uint rawValue) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Devices.Bluetooth.BluetoothClassOfDevice FromParts( global::Windows.Devices.Bluetooth.BluetoothMajorClass majorClass,  global::Windows.Devices.Bluetooth.BluetoothMinorClass minorClass,  global::Windows.Devices.Bluetooth.BluetoothServiceCapabilities serviceCapabilities)
		{
			throw new global::System.NotImplementedException("The member BluetoothClassOfDevice BluetoothClassOfDevice.FromParts(BluetoothMajorClass majorClass, BluetoothMinorClass minorClass, BluetoothServiceCapabilities serviceCapabilities) is not implemented in Uno.");
		}
		#endif
	}
}
