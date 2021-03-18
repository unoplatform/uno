#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AsymmetricKeyAlgorithmProvider 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AlgorithmName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AsymmetricKeyAlgorithmProvider.AlgorithmName is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Cryptography.Core.AsymmetricKeyAlgorithmProvider.AlgorithmName.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Core.CryptographicKey CreateKeyPair( uint keySize)
		{
			throw new global::System.NotImplementedException("The member CryptographicKey AsymmetricKeyAlgorithmProvider.CreateKeyPair(uint keySize) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Core.CryptographicKey ImportKeyPair( global::Windows.Storage.Streams.IBuffer keyBlob)
		{
			throw new global::System.NotImplementedException("The member CryptographicKey AsymmetricKeyAlgorithmProvider.ImportKeyPair(IBuffer keyBlob) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Core.CryptographicKey ImportKeyPair( global::Windows.Storage.Streams.IBuffer keyBlob,  global::Windows.Security.Cryptography.Core.CryptographicPrivateKeyBlobType BlobType)
		{
			throw new global::System.NotImplementedException("The member CryptographicKey AsymmetricKeyAlgorithmProvider.ImportKeyPair(IBuffer keyBlob, CryptographicPrivateKeyBlobType BlobType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Core.CryptographicKey ImportPublicKey( global::Windows.Storage.Streams.IBuffer keyBlob)
		{
			throw new global::System.NotImplementedException("The member CryptographicKey AsymmetricKeyAlgorithmProvider.ImportPublicKey(IBuffer keyBlob) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Core.CryptographicKey ImportPublicKey( global::Windows.Storage.Streams.IBuffer keyBlob,  global::Windows.Security.Cryptography.Core.CryptographicPublicKeyBlobType BlobType)
		{
			throw new global::System.NotImplementedException("The member CryptographicKey AsymmetricKeyAlgorithmProvider.ImportPublicKey(IBuffer keyBlob, CryptographicPublicKeyBlobType BlobType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Core.CryptographicKey CreateKeyPairWithCurveName( string curveName)
		{
			throw new global::System.NotImplementedException("The member CryptographicKey AsymmetricKeyAlgorithmProvider.CreateKeyPairWithCurveName(string curveName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Core.CryptographicKey CreateKeyPairWithCurveParameters( byte[] parameters)
		{
			throw new global::System.NotImplementedException("The member CryptographicKey AsymmetricKeyAlgorithmProvider.CreateKeyPairWithCurveParameters(byte[] parameters) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Cryptography.Core.AsymmetricKeyAlgorithmProvider OpenAlgorithm( string algorithm)
		{
			throw new global::System.NotImplementedException("The member AsymmetricKeyAlgorithmProvider AsymmetricKeyAlgorithmProvider.OpenAlgorithm(string algorithm) is not implemented in Uno.");
		}
		#endif
	}
}
