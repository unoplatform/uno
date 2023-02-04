#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Calls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PhoneLineTransportDevice 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DeviceId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PhoneLineTransportDevice.DeviceId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PhoneLineTransportDevice.DeviceId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Calls.PhoneLineTransport Transport
		{
			get
			{
				throw new global::System.NotImplementedException("The member PhoneLineTransport PhoneLineTransportDevice.Transport is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PhoneLineTransport%20PhoneLineTransportDevice.Transport");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Calls.TransportDeviceAudioRoutingStatus AudioRoutingStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member TransportDeviceAudioRoutingStatus PhoneLineTransportDevice.AudioRoutingStatus is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TransportDeviceAudioRoutingStatus%20PhoneLineTransportDevice.AudioRoutingStatus");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool InBandRingingEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PhoneLineTransportDevice.InBandRingingEnabled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20PhoneLineTransportDevice.InBandRingingEnabled");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneLineTransportDevice.DeviceId.get
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneLineTransportDevice.Transport.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Devices.Enumeration.DeviceAccessStatus> RequestAccessAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceAccessStatus> PhoneLineTransportDevice.RequestAccessAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CDeviceAccessStatus%3E%20PhoneLineTransportDevice.RequestAccessAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RegisterApp()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Calls.PhoneLineTransportDevice", "void PhoneLineTransportDevice.RegisterApp()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RegisterAppForUser( global::Windows.System.User user)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Calls.PhoneLineTransportDevice", "void PhoneLineTransportDevice.RegisterAppForUser(User user)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UnregisterApp()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Calls.PhoneLineTransportDevice", "void PhoneLineTransportDevice.UnregisterApp()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UnregisterAppForUser( global::Windows.System.User user)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Calls.PhoneLineTransportDevice", "void PhoneLineTransportDevice.UnregisterAppForUser(User user)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsRegistered()
		{
			throw new global::System.NotImplementedException("The member bool PhoneLineTransportDevice.IsRegistered() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20PhoneLineTransportDevice.IsRegistered%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Connect()
		{
			throw new global::System.NotImplementedException("The member bool PhoneLineTransportDevice.Connect() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20PhoneLineTransportDevice.Connect%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> ConnectAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> PhoneLineTransportDevice.ConnectAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20PhoneLineTransportDevice.ConnectAsync%28%29");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneLineTransportDevice.AudioRoutingStatus.get
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneLineTransportDevice.AudioRoutingStatusChanged.add
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneLineTransportDevice.AudioRoutingStatusChanged.remove
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneLineTransportDevice.InBandRingingEnabled.get
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneLineTransportDevice.InBandRingingEnabledChanged.add
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneLineTransportDevice.InBandRingingEnabledChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Calls.PhoneLineTransportDevice FromId( string id)
		{
			throw new global::System.NotImplementedException("The member PhoneLineTransportDevice PhoneLineTransportDevice.FromId(string id) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PhoneLineTransportDevice%20PhoneLineTransportDevice.FromId%28string%20id%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector()
		{
			throw new global::System.NotImplementedException("The member string PhoneLineTransportDevice.GetDeviceSelector() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PhoneLineTransportDevice.GetDeviceSelector%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string GetDeviceSelector( global::Windows.ApplicationModel.Calls.PhoneLineTransport transport)
		{
			throw new global::System.NotImplementedException("The member string PhoneLineTransportDevice.GetDeviceSelector(PhoneLineTransport transport) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PhoneLineTransportDevice.GetDeviceSelector%28PhoneLineTransport%20transport%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Calls.PhoneLineTransportDevice, object> AudioRoutingStatusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Calls.PhoneLineTransportDevice", "event TypedEventHandler<PhoneLineTransportDevice, object> PhoneLineTransportDevice.AudioRoutingStatusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Calls.PhoneLineTransportDevice", "event TypedEventHandler<PhoneLineTransportDevice, object> PhoneLineTransportDevice.AudioRoutingStatusChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Calls.PhoneLineTransportDevice, object> InBandRingingEnabledChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Calls.PhoneLineTransportDevice", "event TypedEventHandler<PhoneLineTransportDevice, object> PhoneLineTransportDevice.InBandRingingEnabledChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Calls.PhoneLineTransportDevice", "event TypedEventHandler<PhoneLineTransportDevice, object> PhoneLineTransportDevice.InBandRingingEnabledChanged");
			}
		}
		#endif
	}
}
