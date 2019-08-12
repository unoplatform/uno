#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.Advertisement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class BluetoothLEAdvertisementPublisher 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisement Advertisement
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothLEAdvertisement BluetoothLEAdvertisementPublisher.Advertisement is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisherStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member BluetoothLEAdvertisementPublisherStatus BluetoothLEAdvertisementPublisher.Status is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public BluetoothLEAdvertisementPublisher() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisher", "BluetoothLEAdvertisementPublisher.BluetoothLEAdvertisementPublisher()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisher.BluetoothLEAdvertisementPublisher()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public BluetoothLEAdvertisementPublisher( global::Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisement advertisement) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisher", "BluetoothLEAdvertisementPublisher.BluetoothLEAdvertisementPublisher(BluetoothLEAdvertisement advertisement)");
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisher.BluetoothLEAdvertisementPublisher(Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisement)
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisher.Status.get
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisher.Advertisement.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisher", "void BluetoothLEAdvertisementPublisher.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisher", "void BluetoothLEAdvertisementPublisher.Stop()");
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisher.StatusChanged.add
		// Forced skipping of method Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisher.StatusChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisher, global::Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisherStatusChangedEventArgs> StatusChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisher", "event TypedEventHandler<BluetoothLEAdvertisementPublisher, BluetoothLEAdvertisementPublisherStatusChangedEventArgs> BluetoothLEAdvertisementPublisher.StatusChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementPublisher", "event TypedEventHandler<BluetoothLEAdvertisementPublisher, BluetoothLEAdvertisementPublisherStatusChangedEventArgs> BluetoothLEAdvertisementPublisher.StatusChanged");
			}
		}
		#endif
	}
}
