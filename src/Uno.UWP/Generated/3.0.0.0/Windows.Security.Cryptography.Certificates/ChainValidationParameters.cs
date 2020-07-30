#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Certificates
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ChainValidationParameters 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Networking.HostName ServerDnsName
		{
			get
			{
				throw new global::System.NotImplementedException("The member HostName ChainValidationParameters.ServerDnsName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Cryptography.Certificates.ChainValidationParameters", "HostName ChainValidationParameters.ServerDnsName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Certificates.CertificateChainPolicy CertificateChainPolicy
		{
			get
			{
				throw new global::System.NotImplementedException("The member CertificateChainPolicy ChainValidationParameters.CertificateChainPolicy is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Cryptography.Certificates.ChainValidationParameters", "CertificateChainPolicy ChainValidationParameters.CertificateChainPolicy");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ChainValidationParameters() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Cryptography.Certificates.ChainValidationParameters", "ChainValidationParameters.ChainValidationParameters()");
		}
		#endif
		// Forced skipping of method Windows.Security.Cryptography.Certificates.ChainValidationParameters.ChainValidationParameters()
		// Forced skipping of method Windows.Security.Cryptography.Certificates.ChainValidationParameters.CertificateChainPolicy.get
		// Forced skipping of method Windows.Security.Cryptography.Certificates.ChainValidationParameters.CertificateChainPolicy.set
		// Forced skipping of method Windows.Security.Cryptography.Certificates.ChainValidationParameters.ServerDnsName.get
		// Forced skipping of method Windows.Security.Cryptography.Certificates.ChainValidationParameters.ServerDnsName.set
	}
}
