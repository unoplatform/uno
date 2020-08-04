#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class HashAlgorithmProvider 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AlgorithmName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string HashAlgorithmProvider.AlgorithmName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint HashLength
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint HashAlgorithmProvider.HashLength is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Cryptography.Core.HashAlgorithmProvider.AlgorithmName.get
		// Forced skipping of method Windows.Security.Cryptography.Core.HashAlgorithmProvider.HashLength.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer HashData( global::Windows.Storage.Streams.IBuffer data)
		{
			throw new global::System.NotImplementedException("The member IBuffer HashAlgorithmProvider.HashData(IBuffer data) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Core.CryptographicHash CreateHash()
		{
			throw new global::System.NotImplementedException("The member CryptographicHash HashAlgorithmProvider.CreateHash() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Cryptography.Core.HashAlgorithmProvider OpenAlgorithm( string algorithm)
		{
			throw new global::System.NotImplementedException("The member HashAlgorithmProvider HashAlgorithmProvider.OpenAlgorithm(string algorithm) is not implemented in Uno.");
		}
		#endif
	}
}
