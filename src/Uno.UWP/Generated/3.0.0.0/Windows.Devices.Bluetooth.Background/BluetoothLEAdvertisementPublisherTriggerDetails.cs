#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BluetoothLEAdvertisementPublisherTriggerDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothError Error
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothError BluetoothLEAdvertisementPublisherTriggerDetails.Error is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisherStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothLEAdvertisementPublisherStatus BluetoothLEAdvertisementPublisherTriggerDetails.Status is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  short? SelectedTransmitPowerLevelInDBm
		{
			get
			{
				throw new global::System.NotImplementedException("The member short? BluetoothLEAdvertisementPublisherTriggerDetails.SelectedTransmitPowerLevelInDBm is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.Background.BluetoothLEAdvertisementPublisherTriggerDetails.Status.get
		// Forced skipping of method Windows.Devices.Bluetooth.Background.BluetoothLEAdvertisementPublisherTriggerDetails.Error.get
		// Forced skipping of method Windows.Devices.Bluetooth.Background.BluetoothLEAdvertisementPublisherTriggerDetails.SelectedTransmitPowerLevelInDBm.get
	}
}
