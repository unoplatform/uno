#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email.DataProvider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailMailboxMoveFolderRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string EmailFolderId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxMoveFolderRequest.EmailFolderId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string EmailMailboxId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxMoveFolderRequest.EmailMailboxId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string NewFolderName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxMoveFolderRequest.NewFolderName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string NewParentFolderId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxMoveFolderRequest.NewParentFolderId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxMoveFolderRequest.EmailMailboxId.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxMoveFolderRequest.EmailFolderId.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxMoveFolderRequest.NewParentFolderId.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxMoveFolderRequest.NewFolderName.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction ReportCompletedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxMoveFolderRequest.ReportCompletedAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxMoveFolderRequest.ReportFailedAsync() is not implemented in Uno.");
		}
		#endif
	}
}
