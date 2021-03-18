#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Certificates
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CmsDetachedSignature 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.Certificate> Certificates
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<Certificate> CmsDetachedSignature.Certificates is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.CmsSignerInfo> Signers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<CmsSignerInfo> CmsDetachedSignature.Signers is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public CmsDetachedSignature( global::Windows.Storage.Streams.IBuffer inputBlob) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Cryptography.Certificates.CmsDetachedSignature", "CmsDetachedSignature.CmsDetachedSignature(IBuffer inputBlob)");
		}
		#endif
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CmsDetachedSignature.CmsDetachedSignature(Windows.Storage.Streams.IBuffer)
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CmsDetachedSignature.Certificates.get
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CmsDetachedSignature.Signers.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Security.Cryptography.Certificates.SignatureValidationResult> VerifySignatureAsync( global::Windows.Storage.Streams.IInputStream data)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SignatureValidationResult> CmsDetachedSignature.VerifySignatureAsync(IInputStream data) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Storage.Streams.IBuffer> GenerateSignatureAsync( global::Windows.Storage.Streams.IInputStream data,  global::System.Collections.Generic.IEnumerable<global::Windows.Security.Cryptography.Certificates.CmsSignerInfo> signers,  global::System.Collections.Generic.IEnumerable<global::Windows.Security.Cryptography.Certificates.Certificate> certificates)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IBuffer> CmsDetachedSignature.GenerateSignatureAsync(IInputStream data, IEnumerable<CmsSignerInfo> signers, IEnumerable<Certificate> certificates) is not implemented in Uno.");
		}
		#endif
	}
}
