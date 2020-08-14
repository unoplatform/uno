#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataAccounts
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserDataAccountStore 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.UserDataAccounts.UserDataAccount>> FindAccountsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<UserDataAccount>> UserDataAccountStore.FindAccountsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.UserDataAccounts.UserDataAccount> GetAccountAsync( string id)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<UserDataAccount> UserDataAccountStore.GetAccountAsync(string id) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.UserDataAccounts.UserDataAccount> CreateAccountAsync( string userDisplayName)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<UserDataAccount> UserDataAccountStore.CreateAccountAsync(string userDisplayName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.UserDataAccounts.UserDataAccount> CreateAccountAsync( string userDisplayName,  string packageRelativeAppId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<UserDataAccount> UserDataAccountStore.CreateAccountAsync(string userDisplayName, string packageRelativeAppId) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.UserDataAccounts.UserDataAccountStore.StoreChanged.add
		// Forced skipping of method Windows.ApplicationModel.UserDataAccounts.UserDataAccountStore.StoreChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.UserDataAccounts.UserDataAccount> CreateAccountAsync( string userDisplayName,  string packageRelativeAppId,  string enterpriseId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<UserDataAccount> UserDataAccountStore.CreateAccountAsync(string userDisplayName, string packageRelativeAppId, string enterpriseId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.UserDataAccounts.UserDataAccountStore, global::Windows.ApplicationModel.UserDataAccounts.UserDataAccountStoreChangedEventArgs> StoreChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.UserDataAccounts.UserDataAccountStore", "event TypedEventHandler<UserDataAccountStore, UserDataAccountStoreChangedEventArgs> UserDataAccountStore.StoreChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.UserDataAccounts.UserDataAccountStore", "event TypedEventHandler<UserDataAccountStore, UserDataAccountStoreChangedEventArgs> UserDataAccountStore.StoreChanged");
			}
		}
		#endif
	}
}
