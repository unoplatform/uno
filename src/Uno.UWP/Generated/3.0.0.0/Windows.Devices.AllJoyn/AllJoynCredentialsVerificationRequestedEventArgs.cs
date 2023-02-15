#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.AllJoyn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AllJoynCredentialsVerificationRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.AllJoyn.AllJoynAuthenticationMechanism AuthenticationMechanism
		{
			get
			{
				throw new global::System.NotImplementedException("The member AllJoynAuthenticationMechanism AllJoynCredentialsVerificationRequestedEventArgs.AuthenticationMechanism is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AllJoynAuthenticationMechanism%20AllJoynCredentialsVerificationRequestedEventArgs.AuthenticationMechanism");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Certificates.Certificate PeerCertificate
		{
			get
			{
				throw new global::System.NotImplementedException("The member Certificate AllJoynCredentialsVerificationRequestedEventArgs.PeerCertificate is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Certificate%20AllJoynCredentialsVerificationRequestedEventArgs.PeerCertificate");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.Sockets.SocketSslErrorSeverity PeerCertificateErrorSeverity
		{
			get
			{
				throw new global::System.NotImplementedException("The member SocketSslErrorSeverity AllJoynCredentialsVerificationRequestedEventArgs.PeerCertificateErrorSeverity is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SocketSslErrorSeverity%20AllJoynCredentialsVerificationRequestedEventArgs.PeerCertificateErrorSeverity");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.ChainValidationResult> PeerCertificateErrors
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ChainValidationResult> AllJoynCredentialsVerificationRequestedEventArgs.PeerCertificateErrors is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CChainValidationResult%3E%20AllJoynCredentialsVerificationRequestedEventArgs.PeerCertificateErrors");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.Certificate> PeerIntermediateCertificates
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<Certificate> AllJoynCredentialsVerificationRequestedEventArgs.PeerIntermediateCertificates is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CCertificate%3E%20AllJoynCredentialsVerificationRequestedEventArgs.PeerIntermediateCertificates");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PeerUniqueName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AllJoynCredentialsVerificationRequestedEventArgs.PeerUniqueName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20AllJoynCredentialsVerificationRequestedEventArgs.PeerUniqueName");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentialsVerificationRequestedEventArgs.AuthenticationMechanism.get
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentialsVerificationRequestedEventArgs.PeerUniqueName.get
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentialsVerificationRequestedEventArgs.PeerCertificate.get
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentialsVerificationRequestedEventArgs.PeerCertificateErrorSeverity.get
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentialsVerificationRequestedEventArgs.PeerCertificateErrors.get
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentialsVerificationRequestedEventArgs.PeerIntermediateCertificates.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Accept()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynCredentialsVerificationRequestedEventArgs", "void AllJoynCredentialsVerificationRequestedEventArgs.Accept()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral AllJoynCredentialsVerificationRequestedEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20AllJoynCredentialsVerificationRequestedEventArgs.GetDeferral%28%29");
		}
		#endif
	}
}
