#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GattServiceProviderTrigger : global::Windows.ApplicationModel.Background.IBackgroundTrigger
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattServiceProviderAdvertisingParameters AdvertisingParameters
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattServiceProviderAdvertisingParameters GattServiceProviderTrigger.AdvertisingParameters is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Background.GattServiceProviderTrigger", "GattServiceProviderAdvertisingParameters GattServiceProviderTrigger.AdvertisingParameters");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattLocalService Service
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattLocalService GattServiceProviderTrigger.Service is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string TriggerId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string GattServiceProviderTrigger.TriggerId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Background.GattServiceProviderTrigger.TriggerId.get
		// Forced skipping of method Windows.ApplicationModel.Background.GattServiceProviderTrigger.Service.get
		// Forced skipping of method Windows.ApplicationModel.Background.GattServiceProviderTrigger.AdvertisingParameters.set
		// Forced skipping of method Windows.ApplicationModel.Background.GattServiceProviderTrigger.AdvertisingParameters.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Background.GattServiceProviderTriggerResult> CreateAsync( string triggerId,  global::System.Guid serviceUuid)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<GattServiceProviderTriggerResult> GattServiceProviderTrigger.CreateAsync(string triggerId, Guid serviceUuid) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.ApplicationModel.Background.IBackgroundTrigger
	}
}
