#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DatagramSocket : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.DatagramSocketControl Control
		{
			get
			{
				throw new global::System.NotImplementedException("The member DatagramSocketControl DatagramSocket.Control is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.DatagramSocketInformation Information
		{
			get
			{
				throw new global::System.NotImplementedException("The member DatagramSocketInformation DatagramSocket.Information is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IOutputStream OutputStream
		{
			get
			{
				throw new global::System.NotImplementedException("The member IOutputStream DatagramSocket.OutputStream is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public DatagramSocket() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.DatagramSocket", "DatagramSocket.DatagramSocket()");
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.DatagramSocket.DatagramSocket()
		// Forced skipping of method Windows.Networking.Sockets.DatagramSocket.Control.get
		// Forced skipping of method Windows.Networking.Sockets.DatagramSocket.Information.get
		// Forced skipping of method Windows.Networking.Sockets.DatagramSocket.OutputStream.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ConnectAsync( global::Windows.Networking.HostName remoteHostName,  string remoteServiceName)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction DatagramSocket.ConnectAsync(HostName remoteHostName, string remoteServiceName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ConnectAsync( global::Windows.Networking.EndpointPair endpointPair)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction DatagramSocket.ConnectAsync(EndpointPair endpointPair) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction BindServiceNameAsync( string localServiceName)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction DatagramSocket.BindServiceNameAsync(string localServiceName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction BindEndpointAsync( global::Windows.Networking.HostName localHostName,  string localServiceName)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction DatagramSocket.BindEndpointAsync(HostName localHostName, string localServiceName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void JoinMulticastGroup( global::Windows.Networking.HostName host)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.DatagramSocket", "void DatagramSocket.JoinMulticastGroup(HostName host)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IOutputStream> GetOutputStreamAsync( global::Windows.Networking.HostName remoteHostName,  string remoteServiceName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IOutputStream> DatagramSocket.GetOutputStreamAsync(HostName remoteHostName, string remoteServiceName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IOutputStream> GetOutputStreamAsync( global::Windows.Networking.EndpointPair endpointPair)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IOutputStream> DatagramSocket.GetOutputStreamAsync(EndpointPair endpointPair) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.DatagramSocket.MessageReceived.add
		// Forced skipping of method Windows.Networking.Sockets.DatagramSocket.MessageReceived.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.DatagramSocket", "void DatagramSocket.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction BindServiceNameAsync( string localServiceName,  global::Windows.Networking.Connectivity.NetworkAdapter adapter)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction DatagramSocket.BindServiceNameAsync(string localServiceName, NetworkAdapter adapter) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction CancelIOAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction DatagramSocket.CancelIOAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void EnableTransferOwnership( global::System.Guid taskId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.DatagramSocket", "void DatagramSocket.EnableTransferOwnership(Guid taskId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void EnableTransferOwnership( global::System.Guid taskId,  global::Windows.Networking.Sockets.SocketActivityConnectedStandbyAction connectedStandbyAction)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.DatagramSocket", "void DatagramSocket.EnableTransferOwnership(Guid taskId, SocketActivityConnectedStandbyAction connectedStandbyAction)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void TransferOwnership( string socketId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.DatagramSocket", "void DatagramSocket.TransferOwnership(string socketId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void TransferOwnership( string socketId,  global::Windows.Networking.Sockets.SocketActivityContext data)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.DatagramSocket", "void DatagramSocket.TransferOwnership(string socketId, SocketActivityContext data)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void TransferOwnership( string socketId,  global::Windows.Networking.Sockets.SocketActivityContext data,  global::System.TimeSpan keepAliveTime)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.DatagramSocket", "void DatagramSocket.TransferOwnership(string socketId, SocketActivityContext data, TimeSpan keepAliveTime)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.EndpointPair>> GetEndpointPairsAsync( global::Windows.Networking.HostName remoteHostName,  string remoteServiceName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<EndpointPair>> DatagramSocket.GetEndpointPairsAsync(HostName remoteHostName, string remoteServiceName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Networking.EndpointPair>> GetEndpointPairsAsync( global::Windows.Networking.HostName remoteHostName,  string remoteServiceName,  global::Windows.Networking.HostNameSortOptions sortOptions)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<EndpointPair>> DatagramSocket.GetEndpointPairsAsync(HostName remoteHostName, string remoteServiceName, HostNameSortOptions sortOptions) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Networking.Sockets.DatagramSocket, global::Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs> MessageReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.DatagramSocket", "event TypedEventHandler<DatagramSocket, DatagramSocketMessageReceivedEventArgs> DatagramSocket.MessageReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.DatagramSocket", "event TypedEventHandler<DatagramSocket, DatagramSocketMessageReceivedEventArgs> DatagramSocket.MessageReceived");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
