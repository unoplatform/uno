#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StreamSocketListener : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.StreamSocketListenerControl Control
		{
			get
			{
				throw new global::System.NotImplementedException("The member StreamSocketListenerControl StreamSocketListener.Control is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.StreamSocketListenerInformation Information
		{
			get
			{
				throw new global::System.NotImplementedException("The member StreamSocketListenerInformation StreamSocketListener.Information is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public StreamSocketListener() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocketListener", "StreamSocketListener.StreamSocketListener()");
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListener.StreamSocketListener()
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListener.Control.get
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListener.Information.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction BindServiceNameAsync( string localServiceName)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StreamSocketListener.BindServiceNameAsync(string localServiceName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction BindEndpointAsync( global::Windows.Networking.HostName localHostName,  string localServiceName)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StreamSocketListener.BindEndpointAsync(HostName localHostName, string localServiceName) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListener.ConnectionReceived.add
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListener.ConnectionReceived.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocketListener", "void StreamSocketListener.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction BindServiceNameAsync( string localServiceName,  global::Windows.Networking.Sockets.SocketProtectionLevel protectionLevel)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StreamSocketListener.BindServiceNameAsync(string localServiceName, SocketProtectionLevel protectionLevel) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction BindServiceNameAsync( string localServiceName,  global::Windows.Networking.Sockets.SocketProtectionLevel protectionLevel,  global::Windows.Networking.Connectivity.NetworkAdapter adapter)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StreamSocketListener.BindServiceNameAsync(string localServiceName, SocketProtectionLevel protectionLevel, NetworkAdapter adapter) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction CancelIOAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StreamSocketListener.CancelIOAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void EnableTransferOwnership( global::System.Guid taskId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocketListener", "void StreamSocketListener.EnableTransferOwnership(Guid taskId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void EnableTransferOwnership( global::System.Guid taskId,  global::Windows.Networking.Sockets.SocketActivityConnectedStandbyAction connectedStandbyAction)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocketListener", "void StreamSocketListener.EnableTransferOwnership(Guid taskId, SocketActivityConnectedStandbyAction connectedStandbyAction)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void TransferOwnership( string socketId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocketListener", "void StreamSocketListener.TransferOwnership(string socketId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void TransferOwnership( string socketId,  global::Windows.Networking.Sockets.SocketActivityContext data)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocketListener", "void StreamSocketListener.TransferOwnership(string socketId, SocketActivityContext data)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Networking.Sockets.StreamSocketListener, global::Windows.Networking.Sockets.StreamSocketListenerConnectionReceivedEventArgs> ConnectionReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocketListener", "event TypedEventHandler<StreamSocketListener, StreamSocketListenerConnectionReceivedEventArgs> StreamSocketListener.ConnectionReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocketListener", "event TypedEventHandler<StreamSocketListener, StreamSocketListenerConnectionReceivedEventArgs> StreamSocketListener.ConnectionReceived");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
