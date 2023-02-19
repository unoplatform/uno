#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Credentials
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class KeyCredentialRetrievalResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.KeyCredential Credential
		{
			get
			{
				throw new global::System.NotImplementedException("The member KeyCredential KeyCredentialRetrievalResult.Credential is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=KeyCredential%20KeyCredentialRetrievalResult.Credential");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.KeyCredentialStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member KeyCredentialStatus KeyCredentialRetrievalResult.Status is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=KeyCredentialStatus%20KeyCredentialRetrievalResult.Status");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Credentials.KeyCredentialRetrievalResult.Credential.get
		// Forced skipping of method Windows.Security.Credentials.KeyCredentialRetrievalResult.Status.get
	}
}
