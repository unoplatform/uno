#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email.DataProvider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailMailboxResolveRecipientsRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string EmailMailboxId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxResolveRecipientsRequest.EmailMailboxId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20EmailMailboxResolveRecipientsRequest.EmailMailboxId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> Recipients
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> EmailMailboxResolveRecipientsRequest.Recipients is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3Cstring%3E%20EmailMailboxResolveRecipientsRequest.Recipients");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxResolveRecipientsRequest.EmailMailboxId.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxResolveRecipientsRequest.Recipients.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportCompletedAsync( global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Email.EmailRecipientResolutionResult> resolutionResults)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxResolveRecipientsRequest.ReportCompletedAsync(IEnumerable<EmailRecipientResolutionResult> resolutionResults) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20EmailMailboxResolveRecipientsRequest.ReportCompletedAsync%28IEnumerable%3CEmailRecipientResolutionResult%3E%20resolutionResults%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxResolveRecipientsRequest.ReportFailedAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20EmailMailboxResolveRecipientsRequest.ReportFailedAsync%28%29");
		}
		#endif
	}
}
