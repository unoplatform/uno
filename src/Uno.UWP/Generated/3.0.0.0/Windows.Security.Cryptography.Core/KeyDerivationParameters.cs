#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class KeyDerivationParameters 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer KdfGenericBinary
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer KeyDerivationParameters.KdfGenericBinary is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Cryptography.Core.KeyDerivationParameters", "IBuffer KeyDerivationParameters.KdfGenericBinary");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint IterationCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint KeyDerivationParameters.IterationCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Core.Capi1KdfTargetAlgorithm Capi1KdfTargetAlgorithm
		{
			get
			{
				throw new global::System.NotImplementedException("The member Capi1KdfTargetAlgorithm KeyDerivationParameters.Capi1KdfTargetAlgorithm is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Cryptography.Core.KeyDerivationParameters", "Capi1KdfTargetAlgorithm KeyDerivationParameters.Capi1KdfTargetAlgorithm");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Cryptography.Core.KeyDerivationParameters.KdfGenericBinary.get
		// Forced skipping of method Windows.Security.Cryptography.Core.KeyDerivationParameters.KdfGenericBinary.set
		// Forced skipping of method Windows.Security.Cryptography.Core.KeyDerivationParameters.IterationCount.get
		// Forced skipping of method Windows.Security.Cryptography.Core.KeyDerivationParameters.Capi1KdfTargetAlgorithm.get
		// Forced skipping of method Windows.Security.Cryptography.Core.KeyDerivationParameters.Capi1KdfTargetAlgorithm.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Cryptography.Core.KeyDerivationParameters BuildForCapi1Kdf( global::Windows.Security.Cryptography.Core.Capi1KdfTargetAlgorithm capi1KdfTargetAlgorithm)
		{
			throw new global::System.NotImplementedException("The member KeyDerivationParameters KeyDerivationParameters.BuildForCapi1Kdf(Capi1KdfTargetAlgorithm capi1KdfTargetAlgorithm) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Cryptography.Core.KeyDerivationParameters BuildForPbkdf2( global::Windows.Storage.Streams.IBuffer pbkdf2Salt,  uint iterationCount)
		{
			throw new global::System.NotImplementedException("The member KeyDerivationParameters KeyDerivationParameters.BuildForPbkdf2(IBuffer pbkdf2Salt, uint iterationCount) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Cryptography.Core.KeyDerivationParameters BuildForSP800108( global::Windows.Storage.Streams.IBuffer label,  global::Windows.Storage.Streams.IBuffer context)
		{
			throw new global::System.NotImplementedException("The member KeyDerivationParameters KeyDerivationParameters.BuildForSP800108(IBuffer label, IBuffer context) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Security.Cryptography.Core.KeyDerivationParameters BuildForSP80056a( global::Windows.Storage.Streams.IBuffer algorithmId,  global::Windows.Storage.Streams.IBuffer partyUInfo,  global::Windows.Storage.Streams.IBuffer partyVInfo,  global::Windows.Storage.Streams.IBuffer suppPubInfo,  global::Windows.Storage.Streams.IBuffer suppPrivInfo)
		{
			throw new global::System.NotImplementedException("The member KeyDerivationParameters KeyDerivationParameters.BuildForSP80056a(IBuffer algorithmId, IBuffer partyUInfo, IBuffer partyVInfo, IBuffer suppPubInfo, IBuffer suppPrivInfo) is not implemented in Uno.");
		}
		#endif
	}
}
