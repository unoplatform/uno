#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.Advertisement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BluetoothLEManufacturerData 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer Data
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer BluetoothLEManufacturerData.Data is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Advertisement.BluetoothLEManufacturerData", "IBuffer BluetoothLEManufacturerData.Data");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort CompanyId
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort BluetoothLEManufacturerData.CompanyId is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Advertisement.BluetoothLEManufacturerData", "ushort BluetoothLEManufacturerData.CompanyId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public BluetoothLEManufacturerData() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Advertisement.BluetoothLEManufacturerData", "BluetoothLEManufacturerData.BluetoothLEManufacturerData()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEManufacturerData.BluetoothLEManufacturerData()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public BluetoothLEManufacturerData( ushort companyId,  global::Windows.Storage.Streams.IBuffer data) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Advertisement.BluetoothLEManufacturerData", "BluetoothLEManufacturerData.BluetoothLEManufacturerData(ushort companyId, IBuffer data)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEManufacturerData.BluetoothLEManufacturerData(ushort, Windows.Storage.Streams.IBuffer)
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEManufacturerData.CompanyId.get
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEManufacturerData.CompanyId.set
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEManufacturerData.Data.get
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEManufacturerData.Data.set
	}
}
