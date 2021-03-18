#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailRecipientResolutionResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailRecipientResolutionStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member EmailRecipientResolutionStatus EmailRecipientResolutionResult.Status is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailRecipientResolutionResult", "EmailRecipientResolutionStatus EmailRecipientResolutionResult.Status");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.Certificate> PublicKeys
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<Certificate> EmailRecipientResolutionResult.PublicKeys is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public EmailRecipientResolutionResult() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailRecipientResolutionResult", "EmailRecipientResolutionResult.EmailRecipientResolutionResult()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.EmailRecipientResolutionResult.EmailRecipientResolutionResult()
		// Forced skipping of method Windows.ApplicationModel.Email.EmailRecipientResolutionResult.Status.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailRecipientResolutionResult.PublicKeys.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailRecipientResolutionResult.Status.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetPublicKeys( global::System.Collections.Generic.IEnumerable<global::Windows.Security.Cryptography.Certificates.Certificate> value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailRecipientResolutionResult", "void EmailRecipientResolutionResult.SetPublicKeys(IEnumerable<Certificate> value)");
		}
		#endif
	}
}
