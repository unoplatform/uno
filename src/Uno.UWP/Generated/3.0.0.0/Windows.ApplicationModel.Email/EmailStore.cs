#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailStore 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Email.EmailMailbox>> FindMailboxesAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<EmailMailbox>> EmailStore.FindMailboxesAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIReadOnlyList%3CEmailMailbox%3E%3E%20EmailStore.FindMailboxesAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailConversationReader GetConversationReader()
		{
			throw new global::System.NotImplementedException("The member EmailConversationReader EmailStore.GetConversationReader() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=EmailConversationReader%20EmailStore.GetConversationReader%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailConversationReader GetConversationReader( global::Windows.ApplicationModel.Email.EmailQueryOptions options)
		{
			throw new global::System.NotImplementedException("The member EmailConversationReader EmailStore.GetConversationReader(EmailQueryOptions options) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=EmailConversationReader%20EmailStore.GetConversationReader%28EmailQueryOptions%20options%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailMessageReader GetMessageReader()
		{
			throw new global::System.NotImplementedException("The member EmailMessageReader EmailStore.GetMessageReader() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=EmailMessageReader%20EmailStore.GetMessageReader%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailMessageReader GetMessageReader( global::Windows.ApplicationModel.Email.EmailQueryOptions options)
		{
			throw new global::System.NotImplementedException("The member EmailMessageReader EmailStore.GetMessageReader(EmailQueryOptions options) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=EmailMessageReader%20EmailStore.GetMessageReader%28EmailQueryOptions%20options%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Email.EmailMailbox> GetMailboxAsync( string id)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<EmailMailbox> EmailStore.GetMailboxAsync(string id) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CEmailMailbox%3E%20EmailStore.GetMailboxAsync%28string%20id%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Email.EmailConversation> GetConversationAsync( string id)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<EmailConversation> EmailStore.GetConversationAsync(string id) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CEmailConversation%3E%20EmailStore.GetConversationAsync%28string%20id%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Email.EmailFolder> GetFolderAsync( string id)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<EmailFolder> EmailStore.GetFolderAsync(string id) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CEmailFolder%3E%20EmailStore.GetFolderAsync%28string%20id%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Email.EmailMessage> GetMessageAsync( string id)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<EmailMessage> EmailStore.GetMessageAsync(string id) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CEmailMessage%3E%20EmailStore.GetMessageAsync%28string%20id%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Email.EmailMailbox> CreateMailboxAsync( string accountName,  string accountAddress)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<EmailMailbox> EmailStore.CreateMailboxAsync(string accountName, string accountAddress) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CEmailMailbox%3E%20EmailStore.CreateMailboxAsync%28string%20accountName%2C%20string%20accountAddress%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Email.EmailMailbox> CreateMailboxAsync( string accountName,  string accountAddress,  string userDataAccountId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<EmailMailbox> EmailStore.CreateMailboxAsync(string accountName, string accountAddress, string userDataAccountId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CEmailMailbox%3E%20EmailStore.CreateMailboxAsync%28string%20accountName%2C%20string%20accountAddress%2C%20string%20userDataAccountId%29");
		}
		#endif
	}
}
