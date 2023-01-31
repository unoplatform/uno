#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DatagramSocketMessageReceivedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.HostName LocalAddress
		{
			get
			{
				throw new global::System.NotImplementedException("The member HostName DatagramSocketMessageReceivedEventArgs.LocalAddress is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=HostName%20DatagramSocketMessageReceivedEventArgs.LocalAddress");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.HostName RemoteAddress
		{
			get
			{
				throw new global::System.NotImplementedException("The member HostName DatagramSocketMessageReceivedEventArgs.RemoteAddress is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=HostName%20DatagramSocketMessageReceivedEventArgs.RemoteAddress");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string RemotePort
		{
			get
			{
				throw new global::System.NotImplementedException("The member string DatagramSocketMessageReceivedEventArgs.RemotePort is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20DatagramSocketMessageReceivedEventArgs.RemotePort");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs.RemoteAddress.get
		// Forced skipping of method Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs.RemotePort.get
		// Forced skipping of method Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs.LocalAddress.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.DataReader GetDataReader()
		{
			throw new global::System.NotImplementedException("The member DataReader DatagramSocketMessageReceivedEventArgs.GetDataReader() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DataReader%20DatagramSocketMessageReceivedEventArgs.GetDataReader%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IInputStream GetDataStream()
		{
			throw new global::System.NotImplementedException("The member IInputStream DatagramSocketMessageReceivedEventArgs.GetDataStream() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IInputStream%20DatagramSocketMessageReceivedEventArgs.GetDataStream%28%29");
		}
		#endif
	}
}
