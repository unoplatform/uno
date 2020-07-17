#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataAccounts.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserDataAccountProviderAddAccountOperation : global::Windows.ApplicationModel.UserDataAccounts.Provider.IUserDataAccountProviderOperation
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.UserDataAccounts.UserDataAccountContentKinds ContentKinds
		{
			get
			{
				throw new global::System.NotImplementedException("The member UserDataAccountContentKinds UserDataAccountProviderAddAccountOperation.ContentKinds is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.UserDataAccounts.Provider.UserDataAccountPartnerAccountInfo> PartnerAccountInfos
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<UserDataAccountPartnerAccountInfo> UserDataAccountProviderAddAccountOperation.PartnerAccountInfos is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.UserDataAccounts.Provider.UserDataAccountProviderOperationKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member UserDataAccountProviderOperationKind UserDataAccountProviderAddAccountOperation.Kind is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.UserDataAccounts.Provider.UserDataAccountProviderAddAccountOperation.ContentKinds.get
		// Forced skipping of method Windows.ApplicationModel.UserDataAccounts.Provider.UserDataAccountProviderAddAccountOperation.PartnerAccountInfos.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReportCompleted( string userDataAccountId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.UserDataAccounts.Provider.UserDataAccountProviderAddAccountOperation", "void UserDataAccountProviderAddAccountOperation.ReportCompleted(string userDataAccountId)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.UserDataAccounts.Provider.UserDataAccountProviderAddAccountOperation.Kind.get
		// Processing: Windows.ApplicationModel.UserDataAccounts.Provider.IUserDataAccountProviderOperation
	}
}
