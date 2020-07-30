#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataAccounts.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserDataAccountPartnerAccountInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.UserDataAccounts.Provider.UserDataAccountProviderPartnerAccountKind AccountKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member UserDataAccountProviderPartnerAccountKind UserDataAccountPartnerAccountInfo.AccountKind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string UserDataAccountPartnerAccountInfo.DisplayName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Priority
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UserDataAccountPartnerAccountInfo.Priority is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.UserDataAccounts.Provider.UserDataAccountPartnerAccountInfo.DisplayName.get
		// Forced skipping of method Windows.ApplicationModel.UserDataAccounts.Provider.UserDataAccountPartnerAccountInfo.Priority.get
		// Forced skipping of method Windows.ApplicationModel.UserDataAccounts.Provider.UserDataAccountPartnerAccountInfo.AccountKind.get
	}
}
