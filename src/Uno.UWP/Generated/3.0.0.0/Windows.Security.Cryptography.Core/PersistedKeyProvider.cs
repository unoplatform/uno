#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PersistedKeyProvider 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Cryptography.Core.CryptographicKey> OpenKeyPairFromCertificateAsync( global::Windows.Security.Cryptography.Certificates.Certificate certificate,  string hashAlgorithmName,  global::Windows.Security.Cryptography.Core.CryptographicPadding padding)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<CryptographicKey> PersistedKeyProvider.OpenKeyPairFromCertificateAsync(Certificate certificate, string hashAlgorithmName, CryptographicPadding padding) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Cryptography.Core.CryptographicKey OpenPublicKeyFromCertificate( global::Windows.Security.Cryptography.Certificates.Certificate certificate,  string hashAlgorithmName,  global::Windows.Security.Cryptography.Core.CryptographicPadding padding)
		{
			throw new global::System.NotImplementedException("The member CryptographicKey PersistedKeyProvider.OpenPublicKeyFromCertificate(Certificate certificate, string hashAlgorithmName, CryptographicPadding padding) is not implemented in Uno.");
		}
		#endif
	}
}
