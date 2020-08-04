#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailFolder 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string RemoteId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailFolder.RemoteId is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailFolder", "string EmailFolder.RemoteId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset LastSuccessfulSyncTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset EmailFolder.LastSuccessfulSyncTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailFolder", "DateTimeOffset EmailFolder.LastSuccessfulSyncTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsSyncEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool EmailFolder.IsSyncEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailFolder", "bool EmailFolder.IsSyncEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailFolder.DisplayName is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Email.EmailFolder", "string EmailFolder.DisplayName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailFolder.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailSpecialFolderKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member EmailSpecialFolderKind EmailFolder.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string MailboxId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailFolder.MailboxId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ParentFolderId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailFolder.ParentFolderId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.EmailFolder.Id.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailFolder.RemoteId.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailFolder.RemoteId.set
		// Forced skipping of method Windows.ApplicationModel.Email.EmailFolder.MailboxId.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailFolder.ParentFolderId.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailFolder.DisplayName.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailFolder.DisplayName.set
		// Forced skipping of method Windows.ApplicationModel.Email.EmailFolder.IsSyncEnabled.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailFolder.IsSyncEnabled.set
		// Forced skipping of method Windows.ApplicationModel.Email.EmailFolder.LastSuccessfulSyncTime.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailFolder.LastSuccessfulSyncTime.set
		// Forced skipping of method Windows.ApplicationModel.Email.EmailFolder.Kind.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Email.EmailFolder> CreateFolderAsync( string name)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<EmailFolder> EmailFolder.CreateFolderAsync(string name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction DeleteAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailFolder.DeleteAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Email.EmailFolder>> FindChildFoldersAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<EmailFolder>> EmailFolder.FindChildFoldersAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailConversationReader GetConversationReader()
		{
			throw new global::System.NotImplementedException("The member EmailConversationReader EmailFolder.GetConversationReader() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailConversationReader GetConversationReader( global::Windows.ApplicationModel.Email.EmailQueryOptions options)
		{
			throw new global::System.NotImplementedException("The member EmailConversationReader EmailFolder.GetConversationReader(EmailQueryOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Email.EmailMessage> GetMessageAsync( string id)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<EmailMessage> EmailFolder.GetMessageAsync(string id) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailMessageReader GetMessageReader()
		{
			throw new global::System.NotImplementedException("The member EmailMessageReader EmailFolder.GetMessageReader() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailMessageReader GetMessageReader( global::Windows.ApplicationModel.Email.EmailQueryOptions options)
		{
			throw new global::System.NotImplementedException("The member EmailMessageReader EmailFolder.GetMessageReader(EmailQueryOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Email.EmailItemCounts> GetMessageCountsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<EmailItemCounts> EmailFolder.GetMessageCountsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryMoveAsync( global::Windows.ApplicationModel.Email.EmailFolder newParentFolder)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> EmailFolder.TryMoveAsync(EmailFolder newParentFolder) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryMoveAsync( global::Windows.ApplicationModel.Email.EmailFolder newParentFolder,  string newFolderName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> EmailFolder.TryMoveAsync(EmailFolder newParentFolder, string newFolderName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TrySaveAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> EmailFolder.TrySaveAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SaveMessageAsync( global::Windows.ApplicationModel.Email.EmailMessage message)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailFolder.SaveMessageAsync(EmailMessage message) is not implemented in Uno.");
		}
		#endif
	}
}
