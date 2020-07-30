#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BluetoothLEAdvertisementWatcherTriggerDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementReceivedEventArgs> Advertisements
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<BluetoothLEAdvertisementReceivedEventArgs> BluetoothLEAdvertisementWatcherTriggerDetails.Advertisements is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothError Error
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothError BluetoothLEAdvertisementWatcherTriggerDetails.Error is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.BluetoothSignalStrengthFilter SignalStrengthFilter
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothSignalStrengthFilter BluetoothLEAdvertisementWatcherTriggerDetails.SignalStrengthFilter is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.Background.BluetoothLEAdvertisementWatcherTriggerDetails.Error.get
		// Forced skipping of method Windows.Devices.Bluetooth.Background.BluetoothLEAdvertisementWatcherTriggerDetails.Advertisements.get
		// Forced skipping of method Windows.Devices.Bluetooth.Background.BluetoothLEAdvertisementWatcherTriggerDetails.SignalStrengthFilter.get
	}
}
