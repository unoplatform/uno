#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IWebSocketInformation2 : global::Windows.Networking.Sockets.IWebSocketInformation
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Security.Cryptography.Certificates.Certificate ServerCertificate
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Networking.Sockets.SocketSslErrorSeverity ServerCertificateErrorSeverity
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.ChainValidationResult> ServerCertificateErrors
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.Certificate> ServerIntermediateCertificates
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.IWebSocketInformation2.ServerCertificate.get
		// Forced skipping of method Windows.Networking.Sockets.IWebSocketInformation2.ServerCertificateErrorSeverity.get
		// Forced skipping of method Windows.Networking.Sockets.IWebSocketInformation2.ServerCertificateErrors.get
		// Forced skipping of method Windows.Networking.Sockets.IWebSocketInformation2.ServerIntermediateCertificates.get
	}
}
