#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataAccounts.SystemAccess
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserDataAccountSystemAccessManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction SuppressLocalAccountWithAccountAsync( string userDataAccountId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction UserDataAccountSystemAccessManager.SuppressLocalAccountWithAccountAsync(string userDataAccountId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<string> CreateDeviceAccountAsync( global::Windows.ApplicationModel.UserDataAccounts.SystemAccess.DeviceAccountConfiguration account)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> UserDataAccountSystemAccessManager.CreateDeviceAccountAsync(DeviceAccountConfiguration account) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction DeleteDeviceAccountAsync( string accountId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction UserDataAccountSystemAccessManager.DeleteDeviceAccountAsync(string accountId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.UserDataAccounts.SystemAccess.DeviceAccountConfiguration> GetDeviceAccountConfigurationAsync( string accountId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<DeviceAccountConfiguration> UserDataAccountSystemAccessManager.GetDeviceAccountConfigurationAsync(string accountId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<string>> AddAndShowDeviceAccountsAsync( global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.UserDataAccounts.SystemAccess.DeviceAccountConfiguration> accounts)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<string>> UserDataAccountSystemAccessManager.AddAndShowDeviceAccountsAsync(IEnumerable<DeviceAccountConfiguration> accounts) is not implemented in Uno.");
		}
		#endif
	}
}
