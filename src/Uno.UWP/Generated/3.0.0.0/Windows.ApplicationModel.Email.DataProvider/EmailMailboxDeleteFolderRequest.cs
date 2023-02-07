#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email.DataProvider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailMailboxDeleteFolderRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string EmailFolderId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxDeleteFolderRequest.EmailFolderId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20EmailMailboxDeleteFolderRequest.EmailFolderId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string EmailMailboxId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxDeleteFolderRequest.EmailMailboxId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20EmailMailboxDeleteFolderRequest.EmailMailboxId");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxDeleteFolderRequest.EmailMailboxId.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxDeleteFolderRequest.EmailFolderId.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportCompletedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxDeleteFolderRequest.ReportCompletedAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20EmailMailboxDeleteFolderRequest.ReportCompletedAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync( global::Windows.ApplicationModel.Email.EmailMailboxDeleteFolderStatus status)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxDeleteFolderRequest.ReportFailedAsync(EmailMailboxDeleteFolderStatus status) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20EmailMailboxDeleteFolderRequest.ReportFailedAsync%28EmailMailboxDeleteFolderStatus%20status%29");
		}
		#endif
	}
}
