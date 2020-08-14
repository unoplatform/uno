#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Certificates
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CmsAttachedSignature 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.Certificate> Certificates
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<Certificate> CmsAttachedSignature.Certificates is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  byte[] Content
		{
			get
			{
				throw new global::System.NotImplementedException("The member byte[] CmsAttachedSignature.Content is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.CmsSignerInfo> Signers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<CmsSignerInfo> CmsAttachedSignature.Signers is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public CmsAttachedSignature( global::Windows.Storage.Streams.IBuffer inputBlob) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Cryptography.Certificates.CmsAttachedSignature", "CmsAttachedSignature.CmsAttachedSignature(IBuffer inputBlob)");
		}
		#endif
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CmsAttachedSignature.CmsAttachedSignature(Windows.Storage.Streams.IBuffer)
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CmsAttachedSignature.Certificates.get
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CmsAttachedSignature.Content.get
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CmsAttachedSignature.Signers.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Certificates.SignatureValidationResult VerifySignature()
		{
			throw new global::System.NotImplementedException("The member SignatureValidationResult CmsAttachedSignature.VerifySignature() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IBuffer> GenerateSignatureAsync( global::Windows.Storage.Streams.IBuffer data,  global::System.Collections.Generic.IEnumerable<global::Windows.Security.Cryptography.Certificates.CmsSignerInfo> signers,  global::System.Collections.Generic.IEnumerable<global::Windows.Security.Cryptography.Certificates.Certificate> certificates)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IBuffer> CmsAttachedSignature.GenerateSignatureAsync(IBuffer data, IEnumerable<CmsSignerInfo> signers, IEnumerable<Certificate> certificates) is not implemented in Uno.");
		}
		#endif
	}
}
