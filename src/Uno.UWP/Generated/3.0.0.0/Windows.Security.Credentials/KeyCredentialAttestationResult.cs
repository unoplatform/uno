#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Credentials
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class KeyCredentialAttestationResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer AttestationBuffer
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer KeyCredentialAttestationResult.AttestationBuffer is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer CertificateChainBuffer
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer KeyCredentialAttestationResult.CertificateChainBuffer is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.KeyCredentialAttestationStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member KeyCredentialAttestationStatus KeyCredentialAttestationResult.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Credentials.KeyCredentialAttestationResult.CertificateChainBuffer.get
		// Forced skipping of method Windows.Security.Credentials.KeyCredentialAttestationResult.AttestationBuffer.get
		// Forced skipping of method Windows.Security.Credentials.KeyCredentialAttestationResult.Status.get
	}
}
