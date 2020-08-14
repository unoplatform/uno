#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Bluetooth.GenericAttributeProfile
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class GattWriteRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Offset
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint GattWriteRequest.Offset is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteOption Option
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattWriteOption GattWriteRequest.Option is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattRequestState State
		{
			get
			{
				throw new global::System.NotImplementedException("The member GattRequestState GattWriteRequest.State is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer Value
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer GattWriteRequest.Value is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteRequest.Value.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteRequest.Offset.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteRequest.Option.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteRequest.State.get
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteRequest.StateChanged.add
		// Forced skipping of method Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteRequest.StateChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Respond()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteRequest", "void GattWriteRequest.Respond()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RespondWithProtocolError( byte protocolError)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteRequest", "void GattWriteRequest.RespondWithProtocolError(byte protocolError)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteRequest, global::Windows.Devices.Bluetooth.GenericAttributeProfile.GattRequestStateChangedEventArgs> StateChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteRequest", "event TypedEventHandler<GattWriteRequest, GattRequestStateChangedEventArgs> GattWriteRequest.StateChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Bluetooth.GenericAttributeProfile.GattWriteRequest", "event TypedEventHandler<GattWriteRequest, GattRequestStateChangedEventArgs> GattWriteRequest.StateChanged");
			}
		}
		#endif
	}
}
