#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Certificates
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CertificateStores 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Cryptography.Certificates.CertificateStore IntermediateCertificationAuthorities
		{
			get
			{
				throw new global::System.NotImplementedException("The member CertificateStore CertificateStores.IntermediateCertificationAuthorities is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Cryptography.Certificates.CertificateStore TrustedRootCertificationAuthorities
		{
			get
			{
				throw new global::System.NotImplementedException("The member CertificateStore CertificateStores.TrustedRootCertificationAuthorities is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Cryptography.Certificates.UserCertificateStore GetUserStoreByName( string storeName)
		{
			throw new global::System.NotImplementedException("The member UserCertificateStore CertificateStores.GetUserStoreByName(string storeName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.Certificate>> FindAllAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<Certificate>> CertificateStores.FindAllAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.Certificate>> FindAllAsync( global::Windows.Security.Cryptography.Certificates.CertificateQuery query)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<Certificate>> CertificateStores.FindAllAsync(CertificateQuery query) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CertificateStores.TrustedRootCertificationAuthorities.get
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CertificateStores.IntermediateCertificationAuthorities.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Cryptography.Certificates.CertificateStore GetStoreByName( string storeName)
		{
			throw new global::System.NotImplementedException("The member CertificateStore CertificateStores.GetStoreByName(string storeName) is not implemented in Uno.");
		}
		#endif
	}
}
