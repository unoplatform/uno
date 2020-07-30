#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserPicker 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User SuggestedSelectedUser
		{
			get
			{
				throw new global::System.NotImplementedException("The member User UserPicker.SuggestedSelectedUser is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.UserPicker", "User UserPicker.SuggestedSelectedUser");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AllowGuestAccounts
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool UserPicker.AllowGuestAccounts is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.UserPicker", "bool UserPicker.AllowGuestAccounts");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public UserPicker() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.UserPicker", "UserPicker.UserPicker()");
		}
		#endif
		// Forced skipping of method Windows.System.UserPicker.UserPicker()
		// Forced skipping of method Windows.System.UserPicker.AllowGuestAccounts.get
		// Forced skipping of method Windows.System.UserPicker.AllowGuestAccounts.set
		// Forced skipping of method Windows.System.UserPicker.SuggestedSelectedUser.get
		// Forced skipping of method Windows.System.UserPicker.SuggestedSelectedUser.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.System.User> PickSingleUserAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<User> UserPicker.PickSingleUserAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsSupported()
		{
			throw new global::System.NotImplementedException("The member bool UserPicker.IsSupported() is not implemented in Uno.");
		}
		#endif
	}
}
