#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StreamWebSocket : global::Windows.Networking.Sockets.IWebSocket,global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.StreamWebSocketControl Control
		{
			get
			{
				throw new global::System.NotImplementedException("The member StreamWebSocketControl StreamWebSocket.Control is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.StreamWebSocketInformation Information
		{
			get
			{
				throw new global::System.NotImplementedException("The member StreamWebSocketInformation StreamWebSocket.Information is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IInputStream InputStream
		{
			get
			{
				throw new global::System.NotImplementedException("The member IInputStream StreamWebSocket.InputStream is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IOutputStream OutputStream
		{
			get
			{
				throw new global::System.NotImplementedException("The member IOutputStream StreamWebSocket.OutputStream is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public StreamWebSocket() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamWebSocket", "StreamWebSocket.StreamWebSocket()");
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocket.StreamWebSocket()
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocket.Control.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocket.Information.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocket.InputStream.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocket.OutputStream.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ConnectAsync( global::System.Uri uri)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StreamWebSocket.ConnectAsync(Uri uri) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetRequestHeader( string headerName,  string headerValue)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamWebSocket", "void StreamWebSocket.SetRequestHeader(string headerName, string headerValue)");
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocket.Closed.add
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocket.Closed.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Close( ushort code,  string reason)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamWebSocket", "void StreamWebSocket.Close(ushort code, string reason)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamWebSocket", "void StreamWebSocket.Dispose()");
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocket.ServerCustomValidationRequested.add
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocket.ServerCustomValidationRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Networking.Sockets.IWebSocket, global::Windows.Networking.Sockets.WebSocketClosedEventArgs> Closed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamWebSocket", "event TypedEventHandler<IWebSocket, WebSocketClosedEventArgs> StreamWebSocket.Closed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamWebSocket", "event TypedEventHandler<IWebSocket, WebSocketClosedEventArgs> StreamWebSocket.Closed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Networking.Sockets.StreamWebSocket, global::Windows.Networking.Sockets.WebSocketServerCustomValidationRequestedEventArgs> ServerCustomValidationRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamWebSocket", "event TypedEventHandler<StreamWebSocket, WebSocketServerCustomValidationRequestedEventArgs> StreamWebSocket.ServerCustomValidationRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamWebSocket", "event TypedEventHandler<StreamWebSocket, WebSocketServerCustomValidationRequestedEventArgs> StreamWebSocket.ServerCustomValidationRequested");
			}
		}
		#endif
		// Processing: Windows.Networking.Sockets.IWebSocket
		// Processing: System.IDisposable
	}
}
