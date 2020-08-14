#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ServerMessageWebSocket : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.ServerMessageWebSocketControl Control
		{
			get
			{
				throw new global::System.NotImplementedException("The member ServerMessageWebSocketControl ServerMessageWebSocket.Control is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.ServerMessageWebSocketInformation Information
		{
			get
			{
				throw new global::System.NotImplementedException("The member ServerMessageWebSocketInformation ServerMessageWebSocket.Information is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IOutputStream OutputStream
		{
			get
			{
				throw new global::System.NotImplementedException("The member IOutputStream ServerMessageWebSocket.OutputStream is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.ServerMessageWebSocket.MessageReceived.add
		// Forced skipping of method Windows.Networking.Sockets.ServerMessageWebSocket.MessageReceived.remove
		// Forced skipping of method Windows.Networking.Sockets.ServerMessageWebSocket.Control.get
		// Forced skipping of method Windows.Networking.Sockets.ServerMessageWebSocket.Information.get
		// Forced skipping of method Windows.Networking.Sockets.ServerMessageWebSocket.OutputStream.get
		// Forced skipping of method Windows.Networking.Sockets.ServerMessageWebSocket.Closed.add
		// Forced skipping of method Windows.Networking.Sockets.ServerMessageWebSocket.Closed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Close( ushort code,  string reason)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.ServerMessageWebSocket", "void ServerMessageWebSocket.Close(ushort code, string reason)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.ServerMessageWebSocket", "void ServerMessageWebSocket.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Networking.Sockets.ServerMessageWebSocket, global::Windows.Networking.Sockets.WebSocketClosedEventArgs> Closed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.ServerMessageWebSocket", "event TypedEventHandler<ServerMessageWebSocket, WebSocketClosedEventArgs> ServerMessageWebSocket.Closed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.ServerMessageWebSocket", "event TypedEventHandler<ServerMessageWebSocket, WebSocketClosedEventArgs> ServerMessageWebSocket.Closed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Networking.Sockets.ServerMessageWebSocket, global::Windows.Networking.Sockets.MessageWebSocketMessageReceivedEventArgs> MessageReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.ServerMessageWebSocket", "event TypedEventHandler<ServerMessageWebSocket, MessageWebSocketMessageReceivedEventArgs> ServerMessageWebSocket.MessageReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.ServerMessageWebSocket", "event TypedEventHandler<ServerMessageWebSocket, MessageWebSocketMessageReceivedEventArgs> ServerMessageWebSocket.MessageReceived");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
