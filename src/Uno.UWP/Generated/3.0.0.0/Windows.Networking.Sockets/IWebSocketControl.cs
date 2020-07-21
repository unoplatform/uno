#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IWebSocketControl 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint OutboundBufferSizeInBytes
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Security.Credentials.PasswordCredential ProxyCredential
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Security.Credentials.PasswordCredential ServerCredential
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IList<string> SupportedProtocols
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.IWebSocketControl.OutboundBufferSizeInBytes.get
		// Forced skipping of method Windows.Networking.Sockets.IWebSocketControl.OutboundBufferSizeInBytes.set
		// Forced skipping of method Windows.Networking.Sockets.IWebSocketControl.ServerCredential.get
		// Forced skipping of method Windows.Networking.Sockets.IWebSocketControl.ServerCredential.set
		// Forced skipping of method Windows.Networking.Sockets.IWebSocketControl.ProxyCredential.get
		// Forced skipping of method Windows.Networking.Sockets.IWebSocketControl.ProxyCredential.set
		// Forced skipping of method Windows.Networking.Sockets.IWebSocketControl.SupportedProtocols.get
	}
}
