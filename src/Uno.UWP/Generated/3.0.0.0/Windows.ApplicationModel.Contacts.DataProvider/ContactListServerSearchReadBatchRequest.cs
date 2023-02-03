#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Contacts.DataProvider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ContactListServerSearchReadBatchRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ContactListId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContactListServerSearchReadBatchRequest.ContactListId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20ContactListServerSearchReadBatchRequest.ContactListId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Contacts.ContactQueryOptions Options
		{
			get
			{
				throw new global::System.NotImplementedException("The member ContactQueryOptions ContactListServerSearchReadBatchRequest.Options is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ContactQueryOptions%20ContactListServerSearchReadBatchRequest.Options");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string SessionId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ContactListServerSearchReadBatchRequest.SessionId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20ContactListServerSearchReadBatchRequest.SessionId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint SuggestedBatchSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint ContactListServerSearchReadBatchRequest.SuggestedBatchSize is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20ContactListServerSearchReadBatchRequest.SuggestedBatchSize");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Contacts.DataProvider.ContactListServerSearchReadBatchRequest.SessionId.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.DataProvider.ContactListServerSearchReadBatchRequest.ContactListId.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.DataProvider.ContactListServerSearchReadBatchRequest.Options.get
		// Forced skipping of method Windows.ApplicationModel.Contacts.DataProvider.ContactListServerSearchReadBatchRequest.SuggestedBatchSize.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SaveContactAsync( global::Windows.ApplicationModel.Contacts.Contact contact)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ContactListServerSearchReadBatchRequest.SaveContactAsync(Contact contact) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20ContactListServerSearchReadBatchRequest.SaveContactAsync%28Contact%20contact%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportCompletedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ContactListServerSearchReadBatchRequest.ReportCompletedAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20ContactListServerSearchReadBatchRequest.ReportCompletedAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync( global::Windows.ApplicationModel.Contacts.ContactBatchStatus batchStatus)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ContactListServerSearchReadBatchRequest.ReportFailedAsync(ContactBatchStatus batchStatus) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20ContactListServerSearchReadBatchRequest.ReportFailedAsync%28ContactBatchStatus%20batchStatus%29");
		}
		#endif
	}
}
