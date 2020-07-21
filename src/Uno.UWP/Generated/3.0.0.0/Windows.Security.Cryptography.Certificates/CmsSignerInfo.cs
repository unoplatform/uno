#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Certificates
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CmsSignerInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string HashAlgorithmName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CmsSignerInfo.HashAlgorithmName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Cryptography.Certificates.CmsSignerInfo", "string CmsSignerInfo.HashAlgorithmName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Certificates.Certificate Certificate
		{
			get
			{
				throw new global::System.NotImplementedException("The member Certificate CmsSignerInfo.Certificate is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Cryptography.Certificates.CmsSignerInfo", "Certificate CmsSignerInfo.Certificate");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Cryptography.Certificates.CmsTimestampInfo TimestampInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member CmsTimestampInfo CmsSignerInfo.TimestampInfo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public CmsSignerInfo() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Cryptography.Certificates.CmsSignerInfo", "CmsSignerInfo.CmsSignerInfo()");
		}
		#endif
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CmsSignerInfo.CmsSignerInfo()
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CmsSignerInfo.Certificate.get
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CmsSignerInfo.Certificate.set
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CmsSignerInfo.HashAlgorithmName.get
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CmsSignerInfo.HashAlgorithmName.set
		// Forced skipping of method Windows.Security.Cryptography.Certificates.CmsSignerInfo.TimestampInfo.get
	}
}
