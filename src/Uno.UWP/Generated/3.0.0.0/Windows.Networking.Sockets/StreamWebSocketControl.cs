#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StreamWebSocketControl : global::Windows.Networking.Sockets.IWebSocketControl,global::Windows.Networking.Sockets.IWebSocketControl2
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool NoDelay
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StreamWebSocketControl.NoDelay is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamWebSocketControl", "bool StreamWebSocketControl.NoDelay");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan DesiredUnsolicitedPongInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan StreamWebSocketControl.DesiredUnsolicitedPongInterval is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamWebSocketControl", "TimeSpan StreamWebSocketControl.DesiredUnsolicitedPongInterval");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Certificates.Certificate ClientCertificate
		{
			get
			{
				throw new global::System.NotImplementedException("The member Certificate StreamWebSocketControl.ClientCertificate is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamWebSocketControl", "Certificate StreamWebSocketControl.ClientCertificate");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan ActualUnsolicitedPongInterval
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan StreamWebSocketControl.ActualUnsolicitedPongInterval is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.PasswordCredential ServerCredential
		{
			get
			{
				throw new global::System.NotImplementedException("The member PasswordCredential StreamWebSocketControl.ServerCredential is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamWebSocketControl", "PasswordCredential StreamWebSocketControl.ServerCredential");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.PasswordCredential ProxyCredential
		{
			get
			{
				throw new global::System.NotImplementedException("The member PasswordCredential StreamWebSocketControl.ProxyCredential is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamWebSocketControl", "PasswordCredential StreamWebSocketControl.ProxyCredential");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint OutboundBufferSizeInBytes
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint StreamWebSocketControl.OutboundBufferSizeInBytes is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.StreamWebSocketControl", "uint StreamWebSocketControl.OutboundBufferSizeInBytes");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> SupportedProtocols
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> StreamWebSocketControl.SupportedProtocols is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Security.Cryptography.Certificates.ChainValidationResult> IgnorableServerCertificateErrors
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<ChainValidationResult> StreamWebSocketControl.IgnorableServerCertificateErrors is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketControl.NoDelay.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketControl.NoDelay.set
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketControl.OutboundBufferSizeInBytes.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketControl.OutboundBufferSizeInBytes.set
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketControl.ServerCredential.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketControl.ServerCredential.set
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketControl.ProxyCredential.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketControl.ProxyCredential.set
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketControl.SupportedProtocols.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketControl.IgnorableServerCertificateErrors.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketControl.DesiredUnsolicitedPongInterval.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketControl.DesiredUnsolicitedPongInterval.set
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketControl.ActualUnsolicitedPongInterval.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketControl.ClientCertificate.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketControl.ClientCertificate.set
		// Processing: Windows.Networking.Sockets.IWebSocketControl
		// Processing: Windows.Networking.Sockets.IWebSocketControl2
	}
}
