#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Certificates
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class StandardCertificateStoreNames 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string IntermediateCertificationAuthorities
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StandardCertificateStoreNames.IntermediateCertificationAuthorities is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string Personal
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StandardCertificateStoreNames.Personal is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static string TrustedRootCertificationAuthorities
		{
			get
			{
				throw new global::System.NotImplementedException("The member string StandardCertificateStoreNames.TrustedRootCertificationAuthorities is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Cryptography.Certificates.StandardCertificateStoreNames.Personal.get
		// Forced skipping of method Windows.Security.Cryptography.Certificates.StandardCertificateStoreNames.TrustedRootCertificationAuthorities.get
		// Forced skipping of method Windows.Security.Cryptography.Certificates.StandardCertificateStoreNames.IntermediateCertificationAuthorities.get
	}
}
