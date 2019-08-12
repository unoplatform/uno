#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFiDirect
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WiFiDirectAdvertisementPublisher 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.WiFiDirect.WiFiDirectAdvertisement Advertisement
		{
			get
			{
				throw new global::System.NotImplementedException("The member WiFiDirectAdvertisement WiFiDirectAdvertisementPublisher.Advertisement is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisherStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member WiFiDirectAdvertisementPublisherStatus WiFiDirectAdvertisementPublisher.Status is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public WiFiDirectAdvertisementPublisher() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisher", "WiFiDirectAdvertisementPublisher.WiFiDirectAdvertisementPublisher()");
		}
		#endif
		// Forced skipping of method Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisher.WiFiDirectAdvertisementPublisher()
		// Forced skipping of method Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisher.Advertisement.get
		// Forced skipping of method Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisher.Status.get
		// Forced skipping of method Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisher.StatusChanged.add
		// Forced skipping of method Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisher.StatusChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Start()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisher", "void WiFiDirectAdvertisementPublisher.Start()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisher", "void WiFiDirectAdvertisementPublisher.Stop()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisher, global::Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisherStatusChangedEventArgs> StatusChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisher", "event TypedEventHandler<WiFiDirectAdvertisementPublisher, WiFiDirectAdvertisementPublisherStatusChangedEventArgs> WiFiDirectAdvertisementPublisher.StatusChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisher", "event TypedEventHandler<WiFiDirectAdvertisementPublisher, WiFiDirectAdvertisementPublisherStatusChangedEventArgs> WiFiDirectAdvertisementPublisher.StatusChanged");
			}
		}
		#endif
	}
}
