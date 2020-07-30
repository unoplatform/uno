#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics.DevicePortal
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DevicePortalConnection 
	{
		// Forced skipping of method Windows.System.Diagnostics.DevicePortal.DevicePortalConnection.Closed.add
		// Forced skipping of method Windows.System.Diagnostics.DevicePortal.DevicePortalConnection.Closed.remove
		// Forced skipping of method Windows.System.Diagnostics.DevicePortal.DevicePortalConnection.RequestReceived.add
		// Forced skipping of method Windows.System.Diagnostics.DevicePortal.DevicePortalConnection.RequestReceived.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.ServerMessageWebSocket GetServerMessageWebSocketForRequest( global::Windows.Web.Http.HttpRequestMessage request)
		{
			throw new global::System.NotImplementedException("The member ServerMessageWebSocket DevicePortalConnection.GetServerMessageWebSocketForRequest(HttpRequestMessage request) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.ServerMessageWebSocket GetServerMessageWebSocketForRequest( global::Windows.Web.Http.HttpRequestMessage request,  global::Windows.Networking.Sockets.SocketMessageType messageType,  string protocol)
		{
			throw new global::System.NotImplementedException("The member ServerMessageWebSocket DevicePortalConnection.GetServerMessageWebSocketForRequest(HttpRequestMessage request, SocketMessageType messageType, string protocol) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.ServerMessageWebSocket GetServerMessageWebSocketForRequest( global::Windows.Web.Http.HttpRequestMessage request,  global::Windows.Networking.Sockets.SocketMessageType messageType,  string protocol,  uint outboundBufferSizeInBytes,  uint maxMessageSize,  global::Windows.Networking.Sockets.MessageWebSocketReceiveMode receiveMode)
		{
			throw new global::System.NotImplementedException("The member ServerMessageWebSocket DevicePortalConnection.GetServerMessageWebSocketForRequest(HttpRequestMessage request, SocketMessageType messageType, string protocol, uint outboundBufferSizeInBytes, uint maxMessageSize, MessageWebSocketReceiveMode receiveMode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.ServerStreamWebSocket GetServerStreamWebSocketForRequest( global::Windows.Web.Http.HttpRequestMessage request)
		{
			throw new global::System.NotImplementedException("The member ServerStreamWebSocket DevicePortalConnection.GetServerStreamWebSocketForRequest(HttpRequestMessage request) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.ServerStreamWebSocket GetServerStreamWebSocketForRequest( global::Windows.Web.Http.HttpRequestMessage request,  string protocol,  uint outboundBufferSizeInBytes,  bool noDelay)
		{
			throw new global::System.NotImplementedException("The member ServerStreamWebSocket DevicePortalConnection.GetServerStreamWebSocketForRequest(HttpRequestMessage request, string protocol, uint outboundBufferSizeInBytes, bool noDelay) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Diagnostics.DevicePortal.DevicePortalConnection GetForAppServiceConnection( global::Windows.ApplicationModel.AppService.AppServiceConnection appServiceConnection)
		{
			throw new global::System.NotImplementedException("The member DevicePortalConnection DevicePortalConnection.GetForAppServiceConnection(AppServiceConnection appServiceConnection) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.System.Diagnostics.DevicePortal.DevicePortalConnection, global::Windows.System.Diagnostics.DevicePortal.DevicePortalConnectionClosedEventArgs> Closed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Diagnostics.DevicePortal.DevicePortalConnection", "event TypedEventHandler<DevicePortalConnection, DevicePortalConnectionClosedEventArgs> DevicePortalConnection.Closed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Diagnostics.DevicePortal.DevicePortalConnection", "event TypedEventHandler<DevicePortalConnection, DevicePortalConnectionClosedEventArgs> DevicePortalConnection.Closed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.System.Diagnostics.DevicePortal.DevicePortalConnection, global::Windows.System.Diagnostics.DevicePortal.DevicePortalConnectionRequestReceivedEventArgs> RequestReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Diagnostics.DevicePortal.DevicePortalConnection", "event TypedEventHandler<DevicePortalConnection, DevicePortalConnectionRequestReceivedEventArgs> DevicePortalConnection.RequestReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Diagnostics.DevicePortal.DevicePortalConnection", "event TypedEventHandler<DevicePortalConnection, DevicePortalConnectionRequestReceivedEventArgs> DevicePortalConnection.RequestReceived");
			}
		}
		#endif
	}
}
