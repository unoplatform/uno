#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GattSubscribedClient 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  ushort MaxNotificationSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort GattSubscribedClient.MaxNotificationSize is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattSession Session
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattSession GattSubscribedClient.Session is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattSubscribedClient.Session.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattSubscribedClient.MaxNotificationSize.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattSubscribedClient.MaxNotificationSizeChanged.add
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattSubscribedClient.MaxNotificationSizeChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattSubscribedClient, object> MaxNotificationSizeChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattSubscribedClient", "event TypedEventHandler<GattSubscribedClient, object> GattSubscribedClient.MaxNotificationSizeChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattSubscribedClient", "event TypedEventHandler<GattSubscribedClient, object> GattSubscribedClient.MaxNotificationSizeChanged");
			}
		}
		#endif
	}
}
