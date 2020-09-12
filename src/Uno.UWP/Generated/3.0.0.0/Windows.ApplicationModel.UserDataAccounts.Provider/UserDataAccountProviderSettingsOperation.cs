#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataAccounts.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserDataAccountProviderSettingsOperation : global::Windows.ApplicationModel.UserDataAccounts.Provider.IUserDataAccountProviderOperation
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.UserDataAccounts.Provider.UserDataAccountProviderOperationKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member UserDataAccountProviderOperationKind UserDataAccountProviderSettingsOperation.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string UserDataAccountId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string UserDataAccountProviderSettingsOperation.UserDataAccountId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.UserDataAccounts.Provider.UserDataAccountProviderSettingsOperation.UserDataAccountId.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReportCompleted()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.UserDataAccounts.Provider.UserDataAccountProviderSettingsOperation", "void UserDataAccountProviderSettingsOperation.ReportCompleted()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.UserDataAccounts.Provider.UserDataAccountProviderSettingsOperation.Kind.get
		// Processing: Windows.ApplicationModel.UserDataAccounts.Provider.IUserDataAccountProviderOperation
	}
}
