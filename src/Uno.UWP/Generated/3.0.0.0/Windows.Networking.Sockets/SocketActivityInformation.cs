#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SocketActivityInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.SocketActivityContext Context
		{
			get
			{
				throw new global::System.NotImplementedException("The member SocketActivityContext SocketActivityInformation.Context is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.DatagramSocket DatagramSocket
		{
			get
			{
				throw new global::System.NotImplementedException("The member DatagramSocket SocketActivityInformation.DatagramSocket is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SocketActivityInformation.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.SocketActivityKind SocketKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member SocketActivityKind SocketActivityInformation.SocketKind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.StreamSocket StreamSocket
		{
			get
			{
				throw new global::System.NotImplementedException("The member StreamSocket SocketActivityInformation.StreamSocket is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.StreamSocketListener StreamSocketListener
		{
			get
			{
				throw new global::System.NotImplementedException("The member StreamSocketListener SocketActivityInformation.StreamSocketListener is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid TaskId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid SocketActivityInformation.TaskId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyDictionary<string, global::Windows.Networking.Sockets.SocketActivityInformation> AllSockets
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, SocketActivityInformation> SocketActivityInformation.AllSockets is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.SocketActivityInformation.TaskId.get
		// Forced skipping of method Windows.Networking.Sockets.SocketActivityInformation.Id.get
		// Forced skipping of method Windows.Networking.Sockets.SocketActivityInformation.SocketKind.get
		// Forced skipping of method Windows.Networking.Sockets.SocketActivityInformation.Context.get
		// Forced skipping of method Windows.Networking.Sockets.SocketActivityInformation.DatagramSocket.get
		// Forced skipping of method Windows.Networking.Sockets.SocketActivityInformation.StreamSocket.get
		// Forced skipping of method Windows.Networking.Sockets.SocketActivityInformation.StreamSocketListener.get
		// Forced skipping of method Windows.Networking.Sockets.SocketActivityInformation.AllSockets.get
	}
}
