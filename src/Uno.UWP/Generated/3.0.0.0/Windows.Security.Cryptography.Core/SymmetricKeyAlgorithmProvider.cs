#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SymmetricKeyAlgorithmProvider 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AlgorithmName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SymmetricKeyAlgorithmProvider.AlgorithmName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint BlockLength
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint SymmetricKeyAlgorithmProvider.BlockLength is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Cryptography.Core.SymmetricKeyAlgorithmProvider.AlgorithmName.get
		// Forced skipping of method Windows.Security.Cryptography.Core.SymmetricKeyAlgorithmProvider.BlockLength.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Core.CryptographicKey CreateSymmetricKey( global::Windows.Storage.Streams.IBuffer keyMaterial)
		{
			throw new global::System.NotImplementedException("The member CryptographicKey SymmetricKeyAlgorithmProvider.CreateSymmetricKey(IBuffer keyMaterial) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Cryptography.Core.SymmetricKeyAlgorithmProvider OpenAlgorithm( string algorithm)
		{
			throw new global::System.NotImplementedException("The member SymmetricKeyAlgorithmProvider SymmetricKeyAlgorithmProvider.OpenAlgorithm(string algorithm) is not implemented in Uno.");
		}
		#endif
	}
}
