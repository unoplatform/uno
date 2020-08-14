#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.Advertisement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BluetoothLEAdvertisementDataSection 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte DataType
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte BluetoothLEAdvertisementDataSection.DataType is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementDataSection", "byte BluetoothLEAdvertisementDataSection.DataType");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer Data
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer BluetoothLEAdvertisementDataSection.Data is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementDataSection", "IBuffer BluetoothLEAdvertisementDataSection.Data");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public BluetoothLEAdvertisementDataSection() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementDataSection", "BluetoothLEAdvertisementDataSection.BluetoothLEAdvertisementDataSection()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementDataSection.BluetoothLEAdvertisementDataSection()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public BluetoothLEAdvertisementDataSection( byte dataType,  global::Windows.Storage.Streams.IBuffer data) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementDataSection", "BluetoothLEAdvertisementDataSection.BluetoothLEAdvertisementDataSection(byte dataType, IBuffer data)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementDataSection.BluetoothLEAdvertisementDataSection(byte, Windows.Storage.Streams.IBuffer)
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementDataSection.DataType.get
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementDataSection.DataType.set
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementDataSection.Data.get
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementDataSection.Data.set
	}
}
