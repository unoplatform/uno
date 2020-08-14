#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebSocketServerCustomValidationRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Certificates.Certificate ServerCertificate
		{
			get
			{
				throw new global::System.NotImplementedException("The member Certificate WebSocketServerCustomValidationRequestedEventArgs.ServerCertificate is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.SocketSslErrorSeverity ServerCertificateErrorSeverity
		{
			get
			{
				throw new global::System.NotImplementedException("The member SocketSslErrorSeverity WebSocketServerCustomValidationRequestedEventArgs.ServerCertificateErrorSeverity is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.ChainValidationResult> ServerCertificateErrors
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ChainValidationResult> WebSocketServerCustomValidationRequestedEventArgs.ServerCertificateErrors is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.Certificate> ServerIntermediateCertificates
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<Certificate> WebSocketServerCustomValidationRequestedEventArgs.ServerIntermediateCertificates is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.WebSocketServerCustomValidationRequestedEventArgs.ServerCertificate.get
		// Forced skipping of method Windows.Networking.Sockets.WebSocketServerCustomValidationRequestedEventArgs.ServerCertificateErrorSeverity.get
		// Forced skipping of method Windows.Networking.Sockets.WebSocketServerCustomValidationRequestedEventArgs.ServerCertificateErrors.get
		// Forced skipping of method Windows.Networking.Sockets.WebSocketServerCustomValidationRequestedEventArgs.ServerIntermediateCertificates.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Reject()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Networking.Sockets.WebSocketServerCustomValidationRequestedEventArgs", "void WebSocketServerCustomValidationRequestedEventArgs.Reject()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral WebSocketServerCustomValidationRequestedEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}
