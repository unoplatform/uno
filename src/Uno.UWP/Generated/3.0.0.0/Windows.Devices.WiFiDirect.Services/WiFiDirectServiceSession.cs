#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.WiFiDirect.Services
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WiFiDirectServiceSession : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint AdvertisementId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint WiFiDirectServiceSession.AdvertisementId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSessionErrorStatus ErrorStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member WiFiDirectServiceSessionErrorStatus WiFiDirectServiceSession.ErrorStatus is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ServiceAddress
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WiFiDirectServiceSession.ServiceAddress is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ServiceName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WiFiDirectServiceSession.ServiceName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string SessionAddress
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WiFiDirectServiceSession.SessionAddress is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint SessionId
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint WiFiDirectServiceSession.SessionId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSessionStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member WiFiDirectServiceSessionStatus WiFiDirectServiceSession.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession.ServiceName.get
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession.Status.get
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession.ErrorStatus.get
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession.SessionId.get
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession.AdvertisementId.get
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession.ServiceAddress.get
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession.SessionAddress.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.EndpointPair> GetConnectionEndpointPairs()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<EndpointPair> WiFiDirectServiceSession.GetConnectionEndpointPairs() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession.SessionStatusChanged.add
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession.SessionStatusChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction AddStreamSocketListenerAsync( global::Windows.Networking.Sockets.StreamSocketListener value)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WiFiDirectServiceSession.AddStreamSocketListenerAsync(StreamSocketListener value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction AddDatagramSocketAsync( global::Windows.Networking.Sockets.DatagramSocket value)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction WiFiDirectServiceSession.AddDatagramSocketAsync(DatagramSocket value) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession.RemotePortAdded.add
		// Forced skipping of method Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession.RemotePortAdded.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession", "void WiFiDirectServiceSession.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession, global::Windows.Devices.WiFiDirect.Services.WiFiDirectServiceRemotePortAddedEventArgs> RemotePortAdded
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession", "event TypedEventHandler<WiFiDirectServiceSession, WiFiDirectServiceRemotePortAddedEventArgs> WiFiDirectServiceSession.RemotePortAdded");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession", "event TypedEventHandler<WiFiDirectServiceSession, WiFiDirectServiceRemotePortAddedEventArgs> WiFiDirectServiceSession.RemotePortAdded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession, object> SessionStatusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession", "event TypedEventHandler<WiFiDirectServiceSession, object> WiFiDirectServiceSession.SessionStatusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.WiFiDirect.Services.WiFiDirectServiceSession", "event TypedEventHandler<WiFiDirectServiceSession, object> WiFiDirectServiceSession.SessionStatusChanged");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
