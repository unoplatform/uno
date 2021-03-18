#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email.DataProvider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailMailboxValidateCertificatesRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Security.Cryptography.Certificates.Certificate> Certificates
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<Certificate> EmailMailboxValidateCertificatesRequest.Certificates is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string EmailMailboxId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxValidateCertificatesRequest.EmailMailboxId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxValidateCertificatesRequest.EmailMailboxId.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxValidateCertificatesRequest.Certificates.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportCompletedAsync( global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Email.EmailCertificateValidationStatus> validationStatuses)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxValidateCertificatesRequest.ReportCompletedAsync(IEnumerable<EmailCertificateValidationStatus> validationStatuses) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxValidateCertificatesRequest.ReportFailedAsync() is not implemented in Uno.");
		}
		#endif
	}
}
