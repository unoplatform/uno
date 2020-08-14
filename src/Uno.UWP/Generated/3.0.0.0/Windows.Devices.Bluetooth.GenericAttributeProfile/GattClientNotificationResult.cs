#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GattClientNotificationResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte? ProtocolError
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte? GattClientNotificationResult.ProtocolError is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattCommunicationStatus GattClientNotificationResult.Status is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattSubscribedClient SubscribedClient
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattSubscribedClient GattClientNotificationResult.SubscribedClient is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort BytesSent
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort GattClientNotificationResult.BytesSent is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattClientNotificationResult.SubscribedClient.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattClientNotificationResult.Status.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattClientNotificationResult.ProtocolError.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattClientNotificationResult.BytesSent.get
	}
}
