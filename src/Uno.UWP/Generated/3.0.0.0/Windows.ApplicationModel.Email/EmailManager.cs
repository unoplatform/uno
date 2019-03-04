#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.ApplicationModel.Email.EmailManagerForUser GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member EmailManagerForUser EmailManager.GetForUser(User user) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.Email.EmailStore> RequestStoreAsync( global::Windows.ApplicationModel.Email.EmailStoreAccessType accessType)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<EmailStore> EmailManager.RequestStoreAsync(EmailStoreAccessType accessType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Foundation.IAsyncAction ShowComposeNewEmailAsync( global::Windows.ApplicationModel.Email.EmailMessage message)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailManager.ShowComposeNewEmailAsync(EmailMessage message) is not implemented in Uno.");
		}
		#endif
	}
}
