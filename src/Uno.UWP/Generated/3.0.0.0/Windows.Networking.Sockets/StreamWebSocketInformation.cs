#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Sockets
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StreamWebSocketInformation : global::Windows.Networking.Sockets.IWebSocketInformation,global::Windows.Networking.Sockets.IWebSocketInformation2
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.BandwidthStatistics BandwidthStatistics
		{
			get
			{
				throw new global::System.NotImplementedException("The member BandwidthStatistics StreamWebSocketInformation.BandwidthStatistics is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.HostName LocalAddress
		{
			get
			{
				throw new global::System.NotImplementedException("The member HostName StreamWebSocketInformation.LocalAddress is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Protocol
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StreamWebSocketInformation.Protocol is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Certificates.Certificate ServerCertificate
		{
			get
			{
				throw new global::System.NotImplementedException("The member Certificate StreamWebSocketInformation.ServerCertificate is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.SocketSslErrorSeverity ServerCertificateErrorSeverity
		{
			get
			{
				throw new global::System.NotImplementedException("The member SocketSslErrorSeverity StreamWebSocketInformation.ServerCertificateErrorSeverity is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.ChainValidationResult> ServerCertificateErrors
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ChainValidationResult> StreamWebSocketInformation.ServerCertificateErrors is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.Certificate> ServerIntermediateCertificates
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<Certificate> StreamWebSocketInformation.ServerIntermediateCertificates is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketInformation.LocalAddress.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketInformation.BandwidthStatistics.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketInformation.Protocol.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketInformation.ServerCertificate.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketInformation.ServerCertificateErrorSeverity.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketInformation.ServerCertificateErrors.get
		// Forced skipping of method Windows.Networking.Sockets.StreamWebSocketInformation.ServerIntermediateCertificates.get
		// Processing: Windows.Networking.Sockets.IWebSocketInformation
		// Processing: Windows.Networking.Sockets.IWebSocketInformation2
	}
}
