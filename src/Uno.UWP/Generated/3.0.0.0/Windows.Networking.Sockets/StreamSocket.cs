#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StreamSocket : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.StreamSocketControl Control
		{
			get
			{
				throw new global::System.NotImplementedException("The member StreamSocketControl StreamSocket.Control is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.StreamSocketInformation Information
		{
			get
			{
				throw new global::System.NotImplementedException("The member StreamSocketInformation StreamSocket.Information is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IInputStream InputStream
		{
			get
			{
				throw new global::System.NotImplementedException("The member IInputStream StreamSocket.InputStream is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IOutputStream OutputStream
		{
			get
			{
				throw new global::System.NotImplementedException("The member IOutputStream StreamSocket.OutputStream is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public StreamSocket() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocket", "StreamSocket.StreamSocket()");
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.StreamSocket.StreamSocket()
		// Forced skipping of method Windows.Networking.Sockets.StreamSocket.Control.get
		// Forced skipping of method Windows.Networking.Sockets.StreamSocket.Information.get
		// Forced skipping of method Windows.Networking.Sockets.StreamSocket.InputStream.get
		// Forced skipping of method Windows.Networking.Sockets.StreamSocket.OutputStream.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ConnectAsync( global::Windows.Networking.EndpointPair endpointPair)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StreamSocket.ConnectAsync(EndpointPair endpointPair) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ConnectAsync( global::Windows.Networking.HostName remoteHostName,  string remoteServiceName)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StreamSocket.ConnectAsync(HostName remoteHostName, string remoteServiceName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ConnectAsync( global::Windows.Networking.EndpointPair endpointPair,  global::Windows.Networking.Sockets.SocketProtectionLevel protectionLevel)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StreamSocket.ConnectAsync(EndpointPair endpointPair, SocketProtectionLevel protectionLevel) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ConnectAsync( global::Windows.Networking.HostName remoteHostName,  string remoteServiceName,  global::Windows.Networking.Sockets.SocketProtectionLevel protectionLevel)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StreamSocket.ConnectAsync(HostName remoteHostName, string remoteServiceName, SocketProtectionLevel protectionLevel) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction UpgradeToSslAsync( global::Windows.Networking.Sockets.SocketProtectionLevel protectionLevel,  global::Windows.Networking.HostName validationHostName)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StreamSocket.UpgradeToSslAsync(SocketProtectionLevel protectionLevel, HostName validationHostName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocket", "void StreamSocket.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ConnectAsync( global::Windows.Networking.HostName remoteHostName,  string remoteServiceName,  global::Windows.Networking.Sockets.SocketProtectionLevel protectionLevel,  global::Windows.Networking.Connectivity.NetworkAdapter adapter)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StreamSocket.ConnectAsync(HostName remoteHostName, string remoteServiceName, SocketProtectionLevel protectionLevel, NetworkAdapter adapter) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction CancelIOAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction StreamSocket.CancelIOAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void EnableTransferOwnership( global::System.Guid taskId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocket", "void StreamSocket.EnableTransferOwnership(Guid taskId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void EnableTransferOwnership( global::System.Guid taskId,  global::Windows.Networking.Sockets.SocketActivityConnectedStandbyAction connectedStandbyAction)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocket", "void StreamSocket.EnableTransferOwnership(Guid taskId, SocketActivityConnectedStandbyAction connectedStandbyAction)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void TransferOwnership( string socketId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocket", "void StreamSocket.TransferOwnership(string socketId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void TransferOwnership( string socketId,  global::Windows.Networking.Sockets.SocketActivityContext data)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocket", "void StreamSocket.TransferOwnership(string socketId, SocketActivityContext data)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void TransferOwnership( string socketId,  global::Windows.Networking.Sockets.SocketActivityContext data,  global::System.TimeSpan keepAliveTime)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamSocket", "void StreamSocket.TransferOwnership(string socketId, SocketActivityContext data, TimeSpan keepAliveTime)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.EndpointPair>> GetEndpointPairsAsync( global::Windows.Networking.HostName remoteHostName,  string remoteServiceName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<EndpointPair>> StreamSocket.GetEndpointPairsAsync(HostName remoteHostName, string remoteServiceName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.EndpointPair>> GetEndpointPairsAsync( global::Windows.Networking.HostName remoteHostName,  string remoteServiceName,  global::Windows.Networking.HostNameSortOptions sortOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<EndpointPair>> StreamSocket.GetEndpointPairsAsync(HostName remoteHostName, string remoteServiceName, HostNameSortOptions sortOptions) is not implemented in Uno.");
		}
		#endif
		// Processing: System.IDisposable
	}
}
