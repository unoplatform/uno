#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MessageWebSocketMessageReceivedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.SocketMessageType MessageType
		{
			get
			{
				throw new global::System.NotImplementedException("The member SocketMessageType MessageWebSocketMessageReceivedEventArgs.MessageType is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SocketMessageType%20MessageWebSocketMessageReceivedEventArgs.MessageType");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsMessageComplete
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MessageWebSocketMessageReceivedEventArgs.IsMessageComplete is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20MessageWebSocketMessageReceivedEventArgs.IsMessageComplete");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.MessageWebSocketMessageReceivedEventArgs.MessageType.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.DataReader GetDataReader()
		{
			throw new global::System.NotImplementedException("The member DataReader MessageWebSocketMessageReceivedEventArgs.GetDataReader() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DataReader%20MessageWebSocketMessageReceivedEventArgs.GetDataReader%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IInputStream GetDataStream()
		{
			throw new global::System.NotImplementedException("The member IInputStream MessageWebSocketMessageReceivedEventArgs.GetDataStream() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IInputStream%20MessageWebSocketMessageReceivedEventArgs.GetDataStream%28%29");
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.MessageWebSocketMessageReceivedEventArgs.IsMessageComplete.get
	}
}
