#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email.DataProvider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailMailboxEmptyFolderRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string EmailFolderId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxEmptyFolderRequest.EmailFolderId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20EmailMailboxEmptyFolderRequest.EmailFolderId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string EmailMailboxId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxEmptyFolderRequest.EmailMailboxId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20EmailMailboxEmptyFolderRequest.EmailMailboxId");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxEmptyFolderRequest.EmailMailboxId.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxEmptyFolderRequest.EmailFolderId.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportCompletedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxEmptyFolderRequest.ReportCompletedAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20EmailMailboxEmptyFolderRequest.ReportCompletedAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync( global::Windows.ApplicationModel.Email.EmailMailboxEmptyFolderStatus status)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxEmptyFolderRequest.ReportFailedAsync(EmailMailboxEmptyFolderStatus status) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20EmailMailboxEmptyFolderRequest.ReportFailedAsync%28EmailMailboxEmptyFolderStatus%20status%29");
		}
		#endif
	}
}
