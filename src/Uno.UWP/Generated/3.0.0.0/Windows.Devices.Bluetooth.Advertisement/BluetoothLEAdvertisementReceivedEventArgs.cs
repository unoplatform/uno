#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.Advertisement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BluetoothLEAdvertisementReceivedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisement Advertisement
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothLEAdvertisement BluetoothLEAdvertisementReceivedEventArgs.Advertisement is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementType AdvertisementType
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothLEAdvertisementType BluetoothLEAdvertisementReceivedEventArgs.AdvertisementType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  ulong BluetoothAddress
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong BluetoothLEAdvertisementReceivedEventArgs.BluetoothAddress is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  short RawSignalStrengthInDBm
		{
			get
			{
				throw new global::System.NotImplementedException("The member short BluetoothLEAdvertisementReceivedEventArgs.RawSignalStrengthInDBm is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset Timestamp
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset BluetoothLEAdvertisementReceivedEventArgs.Timestamp is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementReceivedEventArgs.RawSignalStrengthInDBm.get
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementReceivedEventArgs.BluetoothAddress.get
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementReceivedEventArgs.AdvertisementType.get
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementReceivedEventArgs.Timestamp.get
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementReceivedEventArgs.Advertisement.get
	}
}
