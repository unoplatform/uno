#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StreamSocketListenerConnectionReceivedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.StreamSocket Socket
		{
			get
			{
				throw new global::System.NotImplementedException("The member StreamSocket StreamSocketListenerConnectionReceivedEventArgs.Socket is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=StreamSocket%20StreamSocketListenerConnectionReceivedEventArgs.Socket");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.StreamSocketListenerConnectionReceivedEventArgs.Socket.get
	}
}
