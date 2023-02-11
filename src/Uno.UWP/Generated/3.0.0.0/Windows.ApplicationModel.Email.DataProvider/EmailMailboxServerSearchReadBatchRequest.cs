#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email.DataProvider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailMailboxServerSearchReadBatchRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string EmailFolderId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxServerSearchReadBatchRequest.EmailFolderId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20EmailMailboxServerSearchReadBatchRequest.EmailFolderId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string EmailMailboxId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxServerSearchReadBatchRequest.EmailMailboxId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20EmailMailboxServerSearchReadBatchRequest.EmailMailboxId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailQueryOptions Options
		{
			get
			{
				throw new global::System.NotImplementedException("The member EmailQueryOptions EmailMailboxServerSearchReadBatchRequest.Options is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=EmailQueryOptions%20EmailMailboxServerSearchReadBatchRequest.Options");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string SessionId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxServerSearchReadBatchRequest.SessionId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20EmailMailboxServerSearchReadBatchRequest.SessionId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint SuggestedBatchSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint EmailMailboxServerSearchReadBatchRequest.SuggestedBatchSize is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20EmailMailboxServerSearchReadBatchRequest.SuggestedBatchSize");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxServerSearchReadBatchRequest.SessionId.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxServerSearchReadBatchRequest.EmailMailboxId.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxServerSearchReadBatchRequest.EmailFolderId.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxServerSearchReadBatchRequest.Options.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxServerSearchReadBatchRequest.SuggestedBatchSize.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SaveMessageAsync( global::Windows.ApplicationModel.Email.EmailMessage message)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxServerSearchReadBatchRequest.SaveMessageAsync(EmailMessage message) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20EmailMailboxServerSearchReadBatchRequest.SaveMessageAsync%28EmailMessage%20message%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportCompletedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxServerSearchReadBatchRequest.ReportCompletedAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20EmailMailboxServerSearchReadBatchRequest.ReportCompletedAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync( global::Windows.ApplicationModel.Email.EmailBatchStatus batchStatus)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxServerSearchReadBatchRequest.ReportFailedAsync(EmailBatchStatus batchStatus) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20EmailMailboxServerSearchReadBatchRequest.ReportFailedAsync%28EmailBatchStatus%20batchStatus%29");
		}
		#endif
	}
}
