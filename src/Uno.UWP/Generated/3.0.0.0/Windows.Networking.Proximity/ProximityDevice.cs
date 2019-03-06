#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Proximity
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProximityDevice 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  ulong BitsPerSecond
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong ProximityDevice.BitsPerSecond is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ProximityDevice.DeviceId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint MaxMessageBytes
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint ProximityDevice.MaxMessageBytes is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  long SubscribeForMessage( string messageType,  global::Windows.Networking.Proximity.MessageReceivedHandler messageReceivedHandler)
		{
			throw new global::System.NotImplementedException("The member long ProximityDevice.SubscribeForMessage(string messageType, MessageReceivedHandler messageReceivedHandler) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  long PublishMessage( string messageType,  string message)
		{
			throw new global::System.NotImplementedException("The member long ProximityDevice.PublishMessage(string messageType, string message) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  long PublishMessage( string messageType,  string message,  global::Windows.Networking.Proximity.MessageTransmittedHandler messageTransmittedHandler)
		{
			throw new global::System.NotImplementedException("The member long ProximityDevice.PublishMessage(string messageType, string message, MessageTransmittedHandler messageTransmittedHandler) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  long PublishBinaryMessage( string messageType,  global::Windows.Storage.Streams.IBuffer message)
		{
			throw new global::System.NotImplementedException("The member long ProximityDevice.PublishBinaryMessage(string messageType, IBuffer message) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  long PublishBinaryMessage( string messageType,  global::Windows.Storage.Streams.IBuffer message,  global::Windows.Networking.Proximity.MessageTransmittedHandler messageTransmittedHandler)
		{
			throw new global::System.NotImplementedException("The member long ProximityDevice.PublishBinaryMessage(string messageType, IBuffer message, MessageTransmittedHandler messageTransmittedHandler) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  long PublishUriMessage( global::System.Uri message)
		{
			throw new global::System.NotImplementedException("The member long ProximityDevice.PublishUriMessage(Uri message) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  long PublishUriMessage( global::System.Uri message,  global::Windows.Networking.Proximity.MessageTransmittedHandler messageTransmittedHandler)
		{
			throw new global::System.NotImplementedException("The member long ProximityDevice.PublishUriMessage(Uri message, MessageTransmittedHandler messageTransmittedHandler) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void StopSubscribingForMessage( long subscriptionId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Proximity.ProximityDevice", "void ProximityDevice.StopSubscribingForMessage(long subscriptionId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void StopPublishingMessage( long messageId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Proximity.ProximityDevice", "void ProximityDevice.StopPublishingMessage(long messageId)");
		}
		#endif
		// Forced skipping of method Windows.Networking.Proximity.ProximityDevice.DeviceArrived.add
		// Forced skipping of method Windows.Networking.Proximity.ProximityDevice.DeviceArrived.remove
		// Forced skipping of method Windows.Networking.Proximity.ProximityDevice.DeviceDeparted.add
		// Forced skipping of method Windows.Networking.Proximity.ProximityDevice.DeviceDeparted.remove
		// Forced skipping of method Windows.Networking.Proximity.ProximityDevice.MaxMessageBytes.get
		// Forced skipping of method Windows.Networking.Proximity.ProximityDevice.BitsPerSecond.get
		// Forced skipping of method Windows.Networking.Proximity.ProximityDevice.DeviceId.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string ProximityDevice.GetDeviceSelector() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Networking.Proximity.ProximityDevice GetDefault()
		{
			throw new global::System.NotImplementedException("The member ProximityDevice ProximityDevice.GetDefault() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Networking.Proximity.ProximityDevice FromId( string deviceId)
		{
			throw new global::System.NotImplementedException("The member ProximityDevice ProximityDevice.FromId(string deviceId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Networking.Proximity.DeviceArrivedEventHandler DeviceArrived
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Proximity.ProximityDevice", "event DeviceArrivedEventHandler ProximityDevice.DeviceArrived");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Proximity.ProximityDevice", "event DeviceArrivedEventHandler ProximityDevice.DeviceArrived");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Networking.Proximity.DeviceDepartedEventHandler DeviceDeparted
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Proximity.ProximityDevice", "event DeviceDepartedEventHandler ProximityDevice.DeviceDeparted");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Proximity.ProximityDevice", "event DeviceDepartedEventHandler ProximityDevice.DeviceDeparted");
			}
		}
		#endif
	}
}
