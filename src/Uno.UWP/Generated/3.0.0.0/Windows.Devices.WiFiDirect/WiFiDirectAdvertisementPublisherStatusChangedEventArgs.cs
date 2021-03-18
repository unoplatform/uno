#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFiDirect
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WiFiDirectAdvertisementPublisherStatusChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.WiFiDirect.WiFiDirectError Error
		{
			get
			{
				throw new global::System.NotImplementedException("The member WiFiDirectError WiFiDirectAdvertisementPublisherStatusChangedEventArgs.Error is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisherStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member WiFiDirectAdvertisementPublisherStatus WiFiDirectAdvertisementPublisherStatusChangedEventArgs.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisherStatusChangedEventArgs.Status.get
		// Forced skipping of method Windows.Devices.WiFiDirect.WiFiDirectAdvertisementPublisherStatusChangedEventArgs.Error.get
	}
}
