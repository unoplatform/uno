#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GattServiceProvider 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProviderAdvertisementStatus AdvertisementStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattServiceProviderAdvertisementStatus GattServiceProvider.AdvertisementStatus is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalService Service
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattLocalService GattServiceProvider.Service is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProvider.Service.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProvider.AdvertisementStatus.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProvider.AdvertisementStatusChanged.add
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProvider.AdvertisementStatusChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartAdvertising()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProvider", "void GattServiceProvider.StartAdvertising()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartAdvertising( global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProviderAdvertisingParameters parameters)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProvider", "void GattServiceProvider.StartAdvertising(GattServiceProviderAdvertisingParameters parameters)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StopAdvertising()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProvider", "void GattServiceProvider.StopAdvertising()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProviderResult> CreateAsync( global::System.Guid serviceUuid)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattServiceProviderResult> GattServiceProvider.CreateAsync(Guid serviceUuid) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProvider, global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProviderAdvertisementStatusChangedEventArgs> AdvertisementStatusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProvider", "event TypedEventHandler<GattServiceProvider, GattServiceProviderAdvertisementStatusChangedEventArgs> GattServiceProvider.AdvertisementStatusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProvider", "event TypedEventHandler<GattServiceProvider, GattServiceProviderAdvertisementStatusChangedEventArgs> GattServiceProvider.AdvertisementStatusChanged");
			}
		}
		#endif
	}
}
